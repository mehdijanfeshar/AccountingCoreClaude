using System.Text.Json.Serialization;
using Accounting.Application.Common.Exceptions;
using Accounting.Application.Common.Interfaces;
using Accounting.Application.Common.Security;
using Accounting.Domain.Entity;
using Accounting.Domain.ValueObjects;
using FluentValidation;
using MediatR;

namespace Accounting.Application.MonthReopen;

// صورتحساب ماه (تصمیم صاحب پروژه ۲۰۲۶-۱۰-۰۷). ستاد اسناد «بررسی‌شده» یک ماه را برای یک گروه واحد
// (درمانی/بیمه‌ای/ستادی/همه) یا یک واحد «تأیید دائم» می‌کند. واحدی که در آن ماه حتی یک سند غیربررسی‌شده
// (یادداشت/موقت/وضعیت نامعلوم) یا اعلامیهٔ صادرهٔ ارسال‌نشده دارد کلاً صورتحساب نمی‌شود و دلیل در لاگ
// (TB_MONTH_CLOSE_LOG، DDL 074) ثبت می‌شود؛ خود واحد هم لاگش را می‌بیند. واحد بی‌سند/بی‌اعلامیه، یا واحدی
// که فقط سند تأیید دائم دارد، لاگ نمی‌گیرد. پس از «برگشت صورتحساب» همین فرم برای همان یک واحد اجرا می‌شود.

public static class MonthCloseResult
{
    public const int Closed = 1;
    public const int Rejected = 2;
}

public sealed record MonthCloseLogDto(
    Guid Id, Guid BatchId, string VahedCode, string? VahedName, string Year, int Month, int Result,
    int AcceptedCount, int PendingVouchers, int PendingElams, string? Reason, string UserId, DateTime CreatedDate);

public sealed record MonthCloseSummaryDto(
    Guid BatchId, int TargetUnits, int ClosedUnits, int RejectedUnits, int AlreadyClosedUnits, int IdleUnits,
    int AcceptedVouchers, IReadOnlyList<MonthCloseLogDto> Rows);

/// <summary><see cref="UnitCode"/> پُر = فقط همان واحد؛ وگرنه <see cref="UnitCategory"/> (null = همهٔ واحدها).</summary>
public sealed record CloseMonthCommand(string Year, int Month, UnitCategory? UnitCategory, string? UnitCode) : IRequest<MonthCloseSummaryDto>;

public sealed class CloseMonthValidator : AbstractValidator<CloseMonthCommand>
{
    public CloseMonthValidator()
    {
        RuleFor(x => x.Year).NotEmpty().Matches("^[0-9]{4}$").WithMessage("سال مالی باید چهار رقم باشد.");
        RuleFor(x => x.Month).InclusiveBetween(1, 12).WithMessage("ماه باید بین ۱ و ۱۲ باشد.");
        RuleFor(x => x.UnitCategory).IsInEnum();
        RuleFor(x => x.UnitCode).MaximumLength(4);
    }
}

public sealed class CloseMonthHandler : IRequestHandler<CloseMonthCommand, MonthCloseSummaryDto>
{
    private readonly IMonthCloseRepository _repo;
    private readonly IUnitAccessReadRepository _unitAccess;
    private readonly ICurrentUser _currentUser;
    private readonly IUnitOfWork _unitOfWork;

    public CloseMonthHandler(IMonthCloseRepository repo, IUnitAccessReadRepository unitAccess, ICurrentUser currentUser, IUnitOfWork unitOfWork)
    { _repo = repo; _unitAccess = unitAccess; _currentUser = currentUser; _unitOfWork = unitOfWork; }

    public async Task<MonthCloseSummaryDto> Handle(CloseMonthCommand c, CancellationToken ct)
    {
        if (!await MonthReopenRules.IsHeadquartersAdminAsync(_currentUser, _unitAccess, ct))
            throw new RoleAccessDeniedException("صورتحساب ماه فقط برای مدیر ستاد در واحد ستاد مرکزی است.");

        var allUnits = await _unitAccess.GetAllUnitsAsync(ct);
        var names = allUnits.GroupBy(u => u.VahedCode).ToDictionary(g => g.Key, g => g.First().VahedName, StringComparer.Ordinal);

        List<string> targets;
        if (!string.IsNullOrWhiteSpace(c.UnitCode))
        {
            var code = c.UnitCode.Trim();
            if (!names.ContainsKey(code))
                throw new MonthReopenException($"واحد با کد «{code}» پیدا نشد.");
            targets = [code];
        }
        else
        {
            targets = allUnits
                .Where(u => c.UnitCategory is null || UnitCategories.Of(u.TypeCode) == c.UnitCategory)
                .Select(u => u.VahedCode)
                .Distinct(StringComparer.Ordinal)
                .ToList();
        }

        var targetSet = targets.ToHashSet(StringComparer.Ordinal);
        var counts = (await _repo.GetVoucherCountsAsync(c.Year, c.Month, ct))
            .Where(r => targetSet.Contains(r.VahedCode))
            .GroupBy(r => r.VahedCode, StringComparer.Ordinal)
            .ToDictionary(g => g.Key, g => g.ToList(), StringComparer.Ordinal);
        var elams = await _repo.GetUnsentElamCountsAsync(c.Year, c.Month, ct);

        var batchId = Guid.NewGuid();
        var now = DateTime.UtcNow;
        var logs = new List<TB_MONTH_CLOSE_LOG>();
        var toClose = new List<string>();
        int alreadyClosed = 0, idle = 0;

        foreach (var unit in targets)
        {
            var rows = counts.GetValueOrDefault(unit) ?? [];
            var reviewed = rows.Where(r => r.DocLife == DocLife.Reviewed).Sum(r => r.Count);
            var accepted = rows.Where(r => r.DocLife == DocLife.Accepted).Sum(r => r.Count);
            var draft = rows.Where(r => r.DocLife == DocLife.Draft).Sum(r => r.Count);
            var temp = rows.Where(r => r.DocLife == DocLife.Temporary).Sum(r => r.Count);
            var unknown = rows.Where(r => r.DocLife is null or < DocLife.Draft or > DocLife.Accepted).Sum(r => r.Count);
            var pendingVouchers = draft + temp + unknown;
            var pendingElams = elams.GetValueOrDefault(unit);

            if (pendingVouchers > 0 || pendingElams > 0)
            {
                var reasons = new List<string>();
                if (draft > 0) reasons.Add($"{draft} سند یادداشت");
                if (temp > 0) reasons.Add($"{temp} سند موقت");
                if (unknown > 0) reasons.Add($"{unknown} سند با وضعیت نامعلوم");
                if (pendingElams > 0) reasons.Add($"{pendingElams} اعلامیهٔ صادرهٔ ارسال‌نشده");
                logs.Add(Log(unit, MonthCloseResult.Rejected, 0, pendingVouchers, pendingElams,
                    $"صورتحساب نشد: {string.Join("، ", reasons)} در این ماه وجود دارد. همهٔ اسناد باید «بررسی‌شده» و اعلامیه‌ها ارسال شده باشند."));
                continue;
            }

            if (reviewed == 0)
            {
                if (accepted > 0) alreadyClosed++; else idle++;
                continue;
            }

            toClose.Add(unit);
            logs.Add(Log(unit, MonthCloseResult.Closed, reviewed, 0, 0, $"{reviewed} سند بررسی‌شده تأیید دائم شد."));
        }

        var acceptedTotal = 0;
        await _unitOfWork.BeginTransactionAsync(ct);
        try
        {
            if (toClose.Count > 0)
                acceptedTotal = await _repo.AcceptReviewedAsync(c.Year, c.Month, toClose, _currentUser.UserId, ct);
            await _repo.AddLogsAsync(logs, ct);
            await _unitOfWork.SaveChangesAsync(ct);
            await _unitOfWork.CommitTransactionAsync(ct);
        }
        catch
        {
            await _unitOfWork.RollbackTransactionAsync(ct);
            throw;
        }

        return new MonthCloseSummaryDto(
            batchId, targets.Count,
            logs.Count(l => l.RESULT == MonthCloseResult.Closed),
            logs.Count(l => l.RESULT == MonthCloseResult.Rejected),
            alreadyClosed, idle, acceptedTotal,
            logs.Select(l => MonthCloseMapping.ToDto(l, names.GetValueOrDefault(l.VAHEDCODE))).ToList());

        TB_MONTH_CLOSE_LOG Log(string unit, int result, int acceptedCount, int pendingVouchers, int pendingElams, string reason) => new()
        {
            ID = Guid.NewGuid(),
            BATCH_ID = batchId,
            VAHEDCODE = unit,
            YEAR = c.Year,
            MONTH = c.Month,
            RESULT = result,
            ACCEPTED_COUNT = acceptedCount,
            PENDING_VOUCHERS = pendingVouchers,
            PENDING_ELAMS = pendingElams,
            REASON = reason,
            USERID = _currentUser.UserId,
            CREATEDDATE = now,
        };
    }
}

internal static class MonthCloseMapping
{
    public static MonthCloseLogDto ToDto(TB_MONTH_CLOSE_LOG l, string? name) => new(
        l.ID, l.BATCH_ID, l.VAHEDCODE, name, l.YEAR, l.MONTH, l.RESULT, l.ACCEPTED_COUNT,
        l.PENDING_VOUCHERS, l.PENDING_ELAMS, l.REASON, l.USERID, l.CREATEDDATE);
}

/// <summary>لاگ صورتحساب — ستاد همهٔ واحدها (یا یک واحد با <see cref="UnitCode"/>)، بقیه فقط واحد هدر.</summary>
public sealed record GetMonthCloseLogQuery(string Year, int? Month, string? UnitCode) : IRequest<IReadOnlyList<MonthCloseLogDto>>, IVahedScopedQuery
{
    [JsonIgnore]
    public string VahedCode { get; set; } = string.Empty;
}

public sealed class GetMonthCloseLogValidator : AbstractValidator<GetMonthCloseLogQuery>
{
    public GetMonthCloseLogValidator()
    {
        RuleFor(x => x.Year).NotEmpty().Matches("^[0-9]{4}$").WithMessage("سال مالی باید چهار رقم باشد.");
        RuleFor(x => x.Month).InclusiveBetween(1, 12).When(x => x.Month is not null);
        RuleFor(x => x.UnitCode).MaximumLength(4);
    }
}

public sealed class GetMonthCloseLogHandler : IRequestHandler<GetMonthCloseLogQuery, IReadOnlyList<MonthCloseLogDto>>
{
    private readonly IMonthCloseRepository _repo;
    private readonly IUnitAccessReadRepository _unitAccess;
    private readonly ICurrentUser _currentUser;

    public GetMonthCloseLogHandler(IMonthCloseRepository repo, IUnitAccessReadRepository unitAccess, ICurrentUser currentUser)
    { _repo = repo; _unitAccess = unitAccess; _currentUser = currentUser; }

    public async Task<IReadOnlyList<MonthCloseLogDto>> Handle(GetMonthCloseLogQuery q, CancellationToken ct)
    {
        var hq = await MonthReopenRules.IsHeadquartersAdminAsync(_currentUser, _unitAccess, ct);
        var unit = hq ? (string.IsNullOrWhiteSpace(q.UnitCode) ? null : q.UnitCode.Trim()) : q.VahedCode;
        var names = (await _unitAccess.GetAllUnitsAsync(ct))
            .GroupBy(u => u.VahedCode).ToDictionary(g => g.Key, g => g.First().VahedName, StringComparer.Ordinal);
        return (await _repo.GetLogsAsync(q.Year, q.Month, unit, ct))
            .Select(l => MonthCloseMapping.ToDto(l, names.GetValueOrDefault(l.VAHEDCODE)))
            .ToList();
    }
}
