using System.Globalization;
using Accounting.Application.CheckBooks;
using Accounting.Application.Common.Exceptions;
using Accounting.Application.Common.Interfaces;
using Accounting.Application.Common.Security;
using Accounting.Domain.Entity;
using Accounting.Domain.ValueObjects;
using MediatR;

namespace Accounting.Application.Years.Commands.CreateFinancialYear;

/// <summary>
/// «ایجاد سال مالی» (درخواست صاحب پروژه ۲۰۲۶-۱۰-۱۱): هر سال در فروردین، فقط ستاد مرکزی. کار خاص دیگری نمی‌کند:
/// <list type="number">
/// <item>یک ردیف تازه در <c>TB_YEAR</c> با <c>ISCURRENT = 1</c>؛ سال(های) قبل <c>ISCURRENT = 0</c>.</item>
/// <item>برای هر حساب بانکی فعال (همهٔ واحدها) یک دسته‌چک صوری (اعلامیهٔ صوری) سال تازه با همان قالب همیشگی
/// <c>{سال}{کد واحد}0001..1000</c> (<see cref="CheckBookLeaves.SoriRange"/>).</item>
/// </list>
/// مرجع (<c>AddFinancialYearCommandHandler</c> + <c>AddUnitFinancialYearCommandHandler</c>) اسفند را هم می‌پذیرفت
/// و مانده اول دورهٔ حساب‌ها را هم می‌نوشت؛ این‌جا طبق درخواست فقط فروردین و بدون آن. همه در یک تراکنش.
/// اجرای دوباره امن است: دسته‌چک موجود دست نمی‌خورد، حذف‌شدهٔ هم‌کلید بازمی‌گردد (UK_CHECKBOOK).
/// </summary>
public sealed record CreateFinancialYearCommand : IRequest<CreateFinancialYearResult>;

public sealed record CreateFinancialYearResult(
    int Year,
    int? PreviousYear,
    int BankAccounts,
    int CheckBooksCreated,
    int CheckBooksRestored,
    int CheckBooksExisting);

/// <summary>وضعیت برای صفحه: سال قابل ایجاد، اجازه و دلیل عدم اجازه.</summary>
public sealed record GetFinancialYearCreationStatusQuery : IRequest<FinancialYearCreationStatusDto>;

/// <param name="CandidateYear">سال شمسی جاری (سالی که در فروردین ایجاد می‌شود).</param>
/// <param name="CurrentYear">سال پیش‌فرض فعلی (ISCURRENT = 1)، اگر باشد.</param>
/// <param name="CanCreate">همهٔ شرط‌ها برقرار است.</param>
/// <param name="Reason">اگر نه، چرا — متن فارسی برای نمایش.</param>
/// <param name="BankAccounts">تعداد حساب‌های بانکی که دسته‌چک صوری می‌گیرند.</param>
public sealed record FinancialYearCreationStatusDto(
    int CandidateYear,
    int? CurrentYear,
    bool CanCreate,
    string? Reason,
    int BankAccounts);

internal static class FinancialYearRules
{
    public const string HqOnlyMessage = "ایجاد سال مالی فقط برای مدیر ستاد مرکزی ممکن است.";

    public static (int Year, int Month, string Today) Now()
    {
        var calendar = new PersianCalendar();
        var now = DateTime.Now;
        var year = calendar.GetYear(now);
        var month = calendar.GetMonth(now);
        return (year, month, $"{year:0000}{month:00}{calendar.GetDayOfMonth(now):00}");
    }

    /// <summary>null = مجاز؛ وگرنه دلیل.</summary>
    public static string? Check(int year, int month, IReadOnlyCollection<TB_YEAR> years)
    {
        if (month != 1)
            return "سال مالی تازه فقط در فروردین ایجاد می‌شود.";
        if (years.Any(y => y.WORKING_YEAR == year))
            return $"سال مالی {year.ToString(CultureInfo.InvariantCulture)} قبلاً ایجاد شده است.";
        if (years.Any(y => y.WORKING_YEAR > year))
            return "سالی بزرگ‌تر از سال جاری در جدول سال‌ها هست؛ ابتدا آن را بررسی کنید.";
        return null;
    }
}

public sealed class GetFinancialYearCreationStatusQueryHandler
    : IRequestHandler<GetFinancialYearCreationStatusQuery, FinancialYearCreationStatusDto>
{
    private readonly IFinancialYearRepository _repository;
    private readonly IHeadquartersAccessService _hq;

    public GetFinancialYearCreationStatusQueryHandler(IFinancialYearRepository repository, IHeadquartersAccessService hq)
    {
        _repository = repository;
        _hq = hq;
    }

    public async Task<FinancialYearCreationStatusDto> Handle(GetFinancialYearCreationStatusQuery request, CancellationToken cancellationToken)
    {
        if (!await _hq.IsHeadquartersAdminAsync(cancellationToken))
            throw new RoleAccessDeniedException(FinancialYearRules.HqOnlyMessage);

        var (year, month, _) = FinancialYearRules.Now();
        var years = await _repository.GetAllForUpdateAsync(cancellationToken);
        var reason = FinancialYearRules.Check(year, month, years);
        var accounts = await _repository.GetActiveBankAccountsAsync(cancellationToken);
        var current = years.Where(y => y.ISCURRENT == true).Select(y => (int?)y.WORKING_YEAR).Max();
        return new FinancialYearCreationStatusDto(year, current, reason is null, reason, accounts.Count);
    }
}

public sealed class CreateFinancialYearCommandHandler : IRequestHandler<CreateFinancialYearCommand, CreateFinancialYearResult>
{
    private readonly IFinancialYearRepository _repository;
    private readonly IHeadquartersAccessService _hq;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUser _currentUser;

    public CreateFinancialYearCommandHandler(
        IFinancialYearRepository repository, IHeadquartersAccessService hq, IUnitOfWork unitOfWork, ICurrentUser currentUser)
    {
        _repository = repository;
        _hq = hq;
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
    }

    public async Task<CreateFinancialYearResult> Handle(CreateFinancialYearCommand request, CancellationToken cancellationToken)
    {
        if (!await _hq.IsHeadquartersAdminAsync(cancellationToken))
            throw new RoleAccessDeniedException(FinancialYearRules.HqOnlyMessage);

        var (year, month, today) = FinancialYearRules.Now();
        var years = await _repository.GetAllForUpdateAsync(cancellationToken);
        if (FinancialYearRules.Check(year, month, years) is { } reason)
            throw new BusinessRuleException(reason);

        // ۱) سال: قبلی‌ها پیش‌فرض نیستند، تازه پیش‌فرض است. LAST_NUMBER مثل مرجع (۱).
        var previous = years.Where(y => y.ISCURRENT == true).Select(y => (int?)y.WORKING_YEAR).Max();
        foreach (var y in years.Where(y => y.ISCURRENT != false))
            y.ISCURRENT = false;
        _repository.Add(new TB_YEAR { WORKING_YEAR = (short)year, ISCURRENT = true, LAST_NUMBER = 1 });

        // ۲) دسته‌چک صوری سال تازه برای هر حساب بانکی.
        var yearText = year.ToString("0000", CultureInfo.InvariantCulture);
        var accounts = await _repository.GetActiveBankAccountsAsync(cancellationToken);
        var books = await _repository.GetSoriCheckBooksOfYearForUpdateAsync(yearText, cancellationToken);
        var now = DateTime.UtcNow;
        int created = 0, restored = 0, existing = 0;
        foreach (var (accountId, vahedCode) in accounts)
        {
            var (from, to) = CheckBookLeaves.SoriRange(yearText, vahedCode);
            var book = books.FirstOrDefault(b =>
                b.ACCOUNT_ID == accountId && b.FROMCHECKNUMBER == from && b.TOCHECKNUMBER == to && b.VAHEDCODE == vahedCode);
            if (book is { ISDELETED: false })
            {
                existing++;
                continue;
            }

            if (book is not null)
            {
                book.ISDELETED = false;
                book.CHECKBOOK_DATE = today;
                book.CHANGEUSERID = _currentUser.UserId;
                book.UPDATEDDATE = now;
                restored++;
                continue;
            }

            _repository.AddCheckBook(new TB_CHECKBOOK
            {
                ID = Guid.NewGuid(),
                ACCOUNT_ID = accountId,
                CHECKBOOK_TITLE = $"چک صوری {yearText}",
                CHECKBOOK_DATE = today,
                FROMCHECKNUMBER = from,
                TOCHECKNUMBER = to,
                VAHEDCODE = vahedCode,
                CHECKBOOK_TYPE = CheckType.Sori,
                ISDELETED = false,
                ADDUSERID = _currentUser.UserId,
                CREATEDDATE = now,
            });
            created++;
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return new CreateFinancialYearResult(year, previous, accounts.Count, created, restored, existing);
    }
}
