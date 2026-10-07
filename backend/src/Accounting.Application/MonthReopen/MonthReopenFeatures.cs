using System.Security.Cryptography;
using System.Text;
using System.Text.Json.Serialization;
using Accounting.Application.Common.Exceptions;
using Accounting.Application.Common.Interfaces;
using Accounting.Application.Common.Security;
using Accounting.Domain.Entity;
using Accounting.Domain.ValueObjects;
using FluentValidation;
using MediatR;

namespace Accounting.Application.MonthReopen;

// برگشت صورتحساب ماه (تصمیم صاحب پروژه ۲۰۲۶-۱۰-۰۷). ماهی که اسنادش «تأیید دائم» شده صورتحساب‌شده
// است و اسنادش قابل اصلاح نیست. ستاد (مدیر ستاد در واحد ستاد مرکزی) برای (واحد، سال، ماه) رمز صادر
// می‌کند؛ واحد در فرم جدا رمز را وارد می‌کند و اگر درست باشد همهٔ اسناد تأیید دائم آن ماه به
// «بررسی‌شده» برمی‌گردند. رمز ذخیره نمی‌شود و از (واحد، سال، ماه، دفعه) با کلید محرمانه دوباره ساخته
// می‌شود؛ یک‌بار مصرف است و پس از MaxFailedAttempts ورود نادرست باطل می‌شود. DDL 073.

public static class MonthReopenRules
{
    public const int MaxFailedAttempts = 5;

    /// <summary>واحد ستاد مرکزی + نقش مدیر ستاد.</summary>
    public static Task<bool> IsHeadquartersAdminAsync(ICurrentUser user, IUnitAccessReadRepository unitAccess, CancellationToken ct)
        => HeadquartersAccess.IsHeadquartersAdminAsync(user, unitAccess, ct);

    public static bool CodesEqual(string expected, string given)
        => CryptographicOperations.FixedTimeEquals(Encoding.ASCII.GetBytes(expected), Encoding.ASCII.GetBytes(given.Trim()));
}

public sealed record MonthReopenDto(
    Guid Id, string VahedCode, string Year, int Month, int Seq, string? Reason,
    string IssuedBy, DateTime IssuedDate, int FailedAttempts, string? UsedBy, DateTime? UsedDate, int? RevertedCount,
    string Status);

// ---------------------------------------------------------------- صدور رمز (ستاد)

public sealed record IssueMonthReopenCodeCommand(string UnitCode, string Year, int Month, string? Reason) : IRequest<IssueMonthReopenCodeResult>;

public sealed record IssueMonthReopenCodeResult(string Code, int Seq, string UnitCode, string Year, int Month, bool Reused);

public sealed class IssueMonthReopenCodeValidator : AbstractValidator<IssueMonthReopenCodeCommand>
{
    public IssueMonthReopenCodeValidator()
    {
        RuleFor(x => x.UnitCode).NotEmpty().MaximumLength(4);
        RuleFor(x => x.Year).NotEmpty().Matches("^[0-9]{4}$").WithMessage("سال مالی باید چهار رقم باشد.");
        RuleFor(x => x.Month).InclusiveBetween(1, 12).WithMessage("ماه باید بین ۱ و ۱۲ باشد.");
        RuleFor(x => x.Reason).MaximumLength(1000);
    }
}

public sealed class IssueMonthReopenCodeHandler : IRequestHandler<IssueMonthReopenCodeCommand, IssueMonthReopenCodeResult>
{
    private readonly IMonthReopenRepository _repo;
    private readonly IMonthReopenCodeGenerator _generator;
    private readonly IUnitAccessReadRepository _unitAccess;
    private readonly ICurrentUser _currentUser;
    private readonly IUnitOfWork _unitOfWork;

    public IssueMonthReopenCodeHandler(IMonthReopenRepository repo, IMonthReopenCodeGenerator generator,
        IUnitAccessReadRepository unitAccess, ICurrentUser currentUser, IUnitOfWork unitOfWork)
    { _repo = repo; _generator = generator; _unitAccess = unitAccess; _currentUser = currentUser; _unitOfWork = unitOfWork; }

    public async Task<IssueMonthReopenCodeResult> Handle(IssueMonthReopenCodeCommand c, CancellationToken ct)
    {
        if (!await MonthReopenRules.IsHeadquartersAdminAsync(_currentUser, _unitAccess, ct))
            throw new RoleAccessDeniedException("صدور رمز برگشت صورتحساب فقط برای مدیر ستاد در واحد ستاد مرکزی است.");

        var unit = c.UnitCode.Trim();
        if (await _unitAccess.GetUnitProfileAsync(unit, ct) is null)
            throw new MonthReopenException($"واحد با کد «{unit}» پیدا نشد.");

        // رمز صادرشدهٔ مصرف‌نشده دوباره نمایش داده می‌شود، نه رمز تازه — تا دو رمز هم‌زمان باز نباشد.
        var open = await _repo.GetOpenForUpdateAsync(unit, c.Year, c.Month, MonthReopenRules.MaxFailedAttempts, ct);
        if (open is not null)
            return new(_generator.Generate(unit, c.Year, c.Month, open.SEQ), open.SEQ, unit, c.Year, c.Month, true);

        var seq = await _repo.CountAsync(unit, c.Year, c.Month, ct) + 1;
        var code = _generator.Generate(unit, c.Year, c.Month, seq);

        await _repo.AddAsync(new TB_MONTH_REOPEN
        {
            ID = Guid.NewGuid(),
            VAHEDCODE = unit,
            YEAR = c.Year,
            MONTH = c.Month,
            SEQ = seq,
            REASON = string.IsNullOrWhiteSpace(c.Reason) ? null : c.Reason.Trim(),
            ISSUEDBY = _currentUser.UserId,
            ISSUEDDATE = DateTime.UtcNow,
            FAILED_ATTEMPTS = 0,
        }, ct);
        await _unitOfWork.SaveChangesAsync(ct);

        return new(code, seq, unit, c.Year, c.Month, false);
    }
}

// ---------------------------------------------------------------- برگشت صورتحساب (واحد)

public sealed record ApplyMonthReopenCommand(string Year, int Month, string Code) : IRequest<ApplyMonthReopenResult>, IVahedScopedCommand
{
    [JsonIgnore]
    public string VahedCode { get; set; } = string.Empty;
}

public sealed record ApplyMonthReopenResult(int RevertedCount);

public sealed class ApplyMonthReopenValidator : AbstractValidator<ApplyMonthReopenCommand>
{
    public ApplyMonthReopenValidator()
    {
        RuleFor(x => x.Year).NotEmpty().Matches("^[0-9]{4}$").WithMessage("سال مالی باید چهار رقم باشد.");
        RuleFor(x => x.Month).InclusiveBetween(1, 12).WithMessage("ماه باید بین ۱ و ۱۲ باشد.");
        RuleFor(x => x.Code).NotEmpty().Matches("^\\s*[0-9]{8}\\s*$").WithMessage("رمز برگشت باید ۸ رقم باشد.");
    }
}

public sealed class ApplyMonthReopenHandler : IRequestHandler<ApplyMonthReopenCommand, ApplyMonthReopenResult>
{
    private readonly IMonthReopenRepository _repo;
    private readonly IMonthReopenCodeGenerator _generator;
    private readonly ICurrentUser _currentUser;
    private readonly IUnitOfWork _unitOfWork;

    public ApplyMonthReopenHandler(IMonthReopenRepository repo, IMonthReopenCodeGenerator generator,
        ICurrentUser currentUser, IUnitOfWork unitOfWork)
    { _repo = repo; _generator = generator; _currentUser = currentUser; _unitOfWork = unitOfWork; }

    public async Task<ApplyMonthReopenResult> Handle(ApplyMonthReopenCommand c, CancellationToken ct)
    {
        if (!AppRoles.UnitAdminWriters.Any(_currentUser.IsInRole))
            throw new RoleAccessDeniedException("برگشت صورتحساب فقط با نقش‌های مدیریتی واحد ممکن است.");

        var row = await _repo.GetOpenForUpdateAsync(c.VahedCode, c.Year, c.Month, MonthReopenRules.MaxFailedAttempts, ct)
            ?? throw new MonthReopenException("برای این ماه رمز برگشت فعالی از ستاد صادر نشده است.");

        var expected = _generator.Generate(c.VahedCode, c.Year, c.Month, row.SEQ);
        if (!MonthReopenRules.CodesEqual(expected, c.Code))
        {
            row.FAILED_ATTEMPTS++;
            await _unitOfWork.SaveChangesAsync(ct);
            var left = MonthReopenRules.MaxFailedAttempts - row.FAILED_ATTEMPTS;
            throw new MonthReopenException(left > 0
                ? $"رمز برگشت نادرست است. {left} بار دیگر می‌توانید تلاش کنید."
                : "رمز برگشت نادرست است و باطل شد؛ از ستاد رمز تازه بگیرید.");
        }

        var vouchers = await _repo.GetAcceptedVouchersForUpdateAsync(c.VahedCode, c.Year, c.Month, ct);
        if (vouchers.Count == 0)
            throw new MonthReopenException("در این ماه سند «تأیید دائم» وجود ندارد؛ رمز مصرف نشد.");

        var now = DateTime.UtcNow;
        foreach (var v in vouchers)
        {
            v.DOCLIFE = DocLife.Reviewed;
            v.CHANGEUSERID = _currentUser.UserId;
            v.UPDATEDDATE = now;
        }

        row.USEDBY = _currentUser.UserId;
        row.USEDDATE = now;
        row.REVERTED_COUNT = vouchers.Count;

        await _unitOfWork.SaveChangesAsync(ct);
        return new(vouchers.Count);
    }
}

// ---------------------------------------------------------------- سابقه

/// <summary>ستاد همهٔ واحدها را می‌بیند؛ بقیه فقط واحد هدر.</summary>
public sealed record GetMonthReopenLogQuery(string Year) : IRequest<IReadOnlyList<MonthReopenDto>>, IVahedScopedQuery
{
    [JsonIgnore]
    public string VahedCode { get; set; } = string.Empty;
}

public sealed class GetMonthReopenLogValidator : AbstractValidator<GetMonthReopenLogQuery>
{
    public GetMonthReopenLogValidator()
    {
        RuleFor(x => x.Year).NotEmpty().Matches("^[0-9]{4}$").WithMessage("سال مالی باید چهار رقم باشد.");
    }
}

public sealed class GetMonthReopenLogHandler : IRequestHandler<GetMonthReopenLogQuery, IReadOnlyList<MonthReopenDto>>
{
    private readonly IMonthReopenRepository _repo;
    private readonly IUnitAccessReadRepository _unitAccess;
    private readonly ICurrentUser _currentUser;

    public GetMonthReopenLogHandler(IMonthReopenRepository repo, IUnitAccessReadRepository unitAccess, ICurrentUser currentUser)
    { _repo = repo; _unitAccess = unitAccess; _currentUser = currentUser; }

    public async Task<IReadOnlyList<MonthReopenDto>> Handle(GetMonthReopenLogQuery q, CancellationToken ct)
    {
        var all = await MonthReopenRules.IsHeadquartersAdminAsync(_currentUser, _unitAccess, ct);
        var rows = await _repo.GetForYearAsync(q.Year, all ? null : q.VahedCode, ct);
        return rows.Select(r => new MonthReopenDto(
            r.ID, r.VAHEDCODE, r.YEAR, r.MONTH, r.SEQ, r.REASON, r.ISSUEDBY, r.ISSUEDDATE, r.FAILED_ATTEMPTS,
            r.USEDBY, r.USEDDATE, r.REVERTED_COUNT,
            r.USEDDATE is not null ? "Used" : r.FAILED_ATTEMPTS >= MonthReopenRules.MaxFailedAttempts ? "Voided" : "Open"))
            .ToList();
    }
}

/// <summary>همهٔ واحدها (کد + نام) برای انتخاب در فرم صدور رمز — فقط ستاد.</summary>
public sealed record GetMonthReopenUnitsQuery : IRequest<IReadOnlyList<MonthReopenUnitDto>>;

public sealed record MonthReopenUnitDto(string VahedCode, string VahedName);

public sealed class GetMonthReopenUnitsHandler : IRequestHandler<GetMonthReopenUnitsQuery, IReadOnlyList<MonthReopenUnitDto>>
{
    private readonly IUnitAccessReadRepository _unitAccess;
    private readonly ICurrentUser _currentUser;

    public GetMonthReopenUnitsHandler(IUnitAccessReadRepository unitAccess, ICurrentUser currentUser)
    { _unitAccess = unitAccess; _currentUser = currentUser; }

    public async Task<IReadOnlyList<MonthReopenUnitDto>> Handle(GetMonthReopenUnitsQuery q, CancellationToken ct)
    {
        if (!await MonthReopenRules.IsHeadquartersAdminAsync(_currentUser, _unitAccess, ct))
            throw new RoleAccessDeniedException("فهرست واحدها برای صدور رمز فقط برای ستاد مرکزی است.");
        return (await _unitAccess.GetAllUnitsAsync(ct))
            .Select(u => new MonthReopenUnitDto(u.VahedCode, u.VahedName))
            .OrderBy(u => u.VahedCode, StringComparer.Ordinal)
            .ToList();
    }
}

/// <summary>آیا کاربر جاری صادرکنندهٔ رمز است (برای نمایش فرم ستاد در فرانت).</summary>
public sealed record GetMonthReopenAccessQuery : IRequest<MonthReopenAccessDto>;

public sealed record MonthReopenAccessDto(bool CanIssue, bool CanApply);

public sealed class GetMonthReopenAccessHandler : IRequestHandler<GetMonthReopenAccessQuery, MonthReopenAccessDto>
{
    private readonly IUnitAccessReadRepository _unitAccess;
    private readonly ICurrentUser _currentUser;

    public GetMonthReopenAccessHandler(IUnitAccessReadRepository unitAccess, ICurrentUser currentUser)
    { _unitAccess = unitAccess; _currentUser = currentUser; }

    public async Task<MonthReopenAccessDto> Handle(GetMonthReopenAccessQuery q, CancellationToken ct)
        => new(await MonthReopenRules.IsHeadquartersAdminAsync(_currentUser, _unitAccess, ct),
               AppRoles.UnitAdminWriters.Any(_currentUser.IsInRole));
}
