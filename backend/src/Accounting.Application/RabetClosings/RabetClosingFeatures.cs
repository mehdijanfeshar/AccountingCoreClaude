using Accounting.Application.Common.Exceptions;
using Accounting.Application.Common.Interfaces;
using Accounting.Domain.Entity;
using Accounting.Domain.ValueObjects;
using FluentValidation;
using MediatR;

namespace Accounting.Application.RabetClosings;

// رابط اختتامیه (TB_RABET_CLOSING): برای هر سال و نوع واحد، حساب‌های گروه ۶/۷/۸ روی کدام حساب رابط بسته
// شوند (سند اختتامیه، Vouchers.YearEnd). مثل RabetClosingController مرجع، با دو تفاوت عمدی:
// (۱) سال را کاربر انتخاب می‌کند، نه «سال جاری» ثابت؛ (۲) حذف فقط تا وقتی ممکن است که سند اختتامیهٔ آن سال
// برای واحدی از آن نوع صادر نشده باشد. قاعدهٔ مرجع («هر گردشی روی حساب ⇒ حذف ممنوع») عملاً هیچ رابطی را
// قابل حذف نمی‌گذاشت. CRUD مستقل به درخواست صریح صاحب پروژه (۲۰۲۶-۱۰-۰۸؛ مرجع موجودیت §۴ «مبهم»).

public sealed record RabetClosingDto(
    Guid Id, string? Year, Guid VahedTypeId, string? VahedTypeName, RabetAccountLevel? Level,
    Guid? AccountId, string? AccCode, string? AccName, Guid? RabetId, string? RabetCode, string? RabetName, string? Title);

public sealed record RabetClosingAccount(Guid Id, string? AccCode, string? AccName);

public interface IRabetClosingRepository
{
    Task<IReadOnlyList<RabetClosingDto>> ListAsync(string? year, CancellationToken ct);
    Task<IReadOnlyList<RabetClosingAccount>> GetAccountsAsync(IReadOnlyCollection<Guid> ids, CancellationToken ct);
    Task<IReadOnlyList<Guid>> GetExistingVahedTypeIdsAsync(IReadOnlyCollection<Guid> ids, CancellationToken ct);
    /// <summary>ردیف‌های زندهٔ (سال، نوع واحد، حساب) که از قبل هست.</summary>
    Task<IReadOnlyList<(Guid VahedTypeId, Guid AccountId)>> GetExistingPairsAsync(string year, CancellationToken ct);
    Task<TB_RABET_CLOSING?> GetForUpdateAsync(Guid id, CancellationToken ct);
    Task<int> CountClosingVouchersAsync(string year, Guid vahedTypeId, CancellationToken ct);
    Task AddRangeAsync(IEnumerable<TB_RABET_CLOSING> rows, CancellationToken ct);
}

public sealed record GetRabetClosingsQuery(string? Year) : IRequest<IReadOnlyList<RabetClosingDto>>;

public sealed class GetRabetClosingsHandler(IRabetClosingRepository repo) : IRequestHandler<GetRabetClosingsQuery, IReadOnlyList<RabetClosingDto>>
{
    public Task<IReadOnlyList<RabetClosingDto>> Handle(GetRabetClosingsQuery q, CancellationToken ct) => repo.ListAsync(q.Year, ct);
}

/// <summary>یک ردیف به ازای هر (نوع واحد × حساب): همان رفتار مرجع که چند حساب و چند نوع واحد را یک‌جا می‌گیرد.</summary>
public sealed record CreateRabetClosingsCommand(
    string Year, IReadOnlyList<Guid> VahedTypeIds, IReadOnlyList<Guid> AccountIds, Guid RabetAccountId, string Title) : IRequest<int>;

public sealed class CreateRabetClosingsValidator : AbstractValidator<CreateRabetClosingsCommand>
{
    public CreateRabetClosingsValidator()
    {
        RuleFor(x => x.Year).NotEmpty().Matches("^[0-9]{4}$").WithMessage("سال مالی باید چهار رقم باشد.");
        RuleFor(x => x.VahedTypeIds).NotEmpty().WithMessage("حداقل یک نوع واحد را انتخاب کنید.");
        RuleFor(x => x.AccountIds).NotEmpty().WithMessage("حداقل یک حساب را انتخاب کنید.");
        RuleFor(x => x.RabetAccountId).NotEmpty().WithMessage("حساب رابط را انتخاب کنید.");
        RuleFor(x => x.Title).NotEmpty().WithMessage("عنوان الزامی است.").MaximumLength(200);
    }
}

public sealed class CreateRabetClosingsHandler(IRabetClosingRepository repo, ICurrentUser currentUser, IUnitOfWork unitOfWork)
    : IRequestHandler<CreateRabetClosingsCommand, int>
{
    public async Task<int> Handle(CreateRabetClosingsCommand c, CancellationToken ct)
    {
        var accountIds = c.AccountIds.Distinct().ToList();
        if (accountIds.Contains(c.RabetAccountId))
            throw new BusinessRuleException("حساب و حساب رابط نمی‌توانند یکی باشند.");

        var accounts = await repo.GetAccountsAsync([.. accountIds, c.RabetAccountId], ct);
        var rabet = accounts.FirstOrDefault(a => a.Id == c.RabetAccountId)
            ?? throw new BusinessRuleException("حساب رابط پیدا نشد.");
        if (rabet.AccCode?.Length != 6)
            throw new BusinessRuleException("حساب رابط باید معین (کد ۶ رقمی) باشد.");

        var levels = new Dictionary<Guid, RabetAccountLevel>();
        foreach (var id in accountIds)
        {
            var account = accounts.FirstOrDefault(a => a.Id == id)
                ?? throw new BusinessRuleException("یکی از حساب‌های انتخاب‌شده پیدا نشد.");
            levels[id] = account.AccCode?.Length switch
            {
                4 => RabetAccountLevel.Kol,
                6 => RabetAccountLevel.Moein,
                _ => throw new BusinessRuleException($"حساب {account.AccCode} کل یا معین نیست؛ فقط کل (۴ رقم) یا معین (۶ رقم) پذیرفته می‌شود."),
            };
        }

        var vahedTypeIds = c.VahedTypeIds.Distinct().ToList();
        var knownTypes = await repo.GetExistingVahedTypeIdsAsync(vahedTypeIds, ct);
        if (knownTypes.Count != vahedTypeIds.Count)
            throw new BusinessRuleException("یکی از نوع‌های واحد انتخاب‌شده پیدا نشد.");

        var existing = (await repo.GetExistingPairsAsync(c.Year, ct)).ToHashSet();
        var duplicates = vahedTypeIds.SelectMany(t => accountIds.Select(a => (t, a))).Where(existing.Contains).ToList();
        if (duplicates.Count > 0)
        {
            var codes = duplicates.Select(d => accounts.First(a => a.Id == d.a).AccCode).Distinct();
            throw new BusinessRuleException($"برای حساب {string.Join("، ", codes)} در سال {c.Year} و همین نوع واحد، رابط اختتامیه از قبل تعریف شده است.");
        }

        var now = DateTime.UtcNow;
        var rows = vahedTypeIds.SelectMany(t => accountIds.Select(a => new TB_RABET_CLOSING
        {
            ID = Guid.NewGuid(),
            TYPEACCOUNTCODE = levels[a],
            VAHEDTYPE_ID = t,
            ACCOUNTCODE_ID = a,
            ACCOUNTCODE_RABET_ID = c.RabetAccountId,
            TITLE = c.Title.Trim(),
            YEAR = c.Year,
            ADDUSERID = currentUser.UserId,
            CREATEDDATE = now,
            ISDELETED = false,
        })).ToList();

        await repo.AddRangeAsync(rows, ct);
        await unitOfWork.SaveChangesAsync(ct);
        return rows.Count;
    }
}

public sealed record DeleteRabetClosingCommand(Guid Id) : IRequest<Unit>;

public sealed class DeleteRabetClosingHandler(IRabetClosingRepository repo, ICurrentUser currentUser, IUnitOfWork unitOfWork)
    : IRequestHandler<DeleteRabetClosingCommand, Unit>
{
    public async Task<Unit> Handle(DeleteRabetClosingCommand c, CancellationToken ct)
    {
        var row = await repo.GetForUpdateAsync(c.Id, ct) ?? throw new NotFoundException("رابط اختتامیه", c.Id);
        if (row.ISDELETED == true)
            return Unit.Value;
        if (!string.IsNullOrEmpty(row.YEAR) && await repo.CountClosingVouchersAsync(row.YEAR, row.VAHEDTYPE_ID, ct) > 0)
            throw new BusinessRuleException($"سند اختتامیهٔ سال {row.YEAR} برای واحدی از این نوع صادر شده است؛ تا آن سند هست این رابط حذف نمی‌شود.");

        row.ISDELETED = true;
        row.CHANGEUSERID = currentUser.UserId;
        row.UPDATEDDATE = DateTime.UtcNow;
        await unitOfWork.SaveChangesAsync(ct);
        return Unit.Value;
    }
}
