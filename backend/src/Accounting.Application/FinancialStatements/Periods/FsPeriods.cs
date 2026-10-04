using Accounting.Application.FinancialStatements.Access;
using System.Text.Json.Serialization;
using Accounting.Application.Common.Exceptions;
using Accounting.Application.Common.Interfaces;
using Accounting.Application.Common.Security;
using Accounting.Application.FinancialStatements.Queries;
using Accounting.Domain.Entity;
using Accounting.Domain.ValueObjects;
using FluentValidation;
using MediatR;

namespace Accounting.Application.FinancialStatements.Periods;

// ح-۵ (docs/fs-module.md §۱۳) — بستن دورهٔ صورت‌ها (سند منبع §۱۱ و §۱۲-۳ «بستن دوره و گردش تأیید»).
// تصمیم صاحب پروژه: فقط برای صورت‌ها — کنترل V-11 هنگام انتشار؛ ثبت سند دست نمی‌خورد.
// دوره = سال مالی. باز ← بستهٔ موقت ← قفل. قفل واحد، زیرمجموعه‌اش را هم قفل‌شده حساب می‌کند.

/// <summary>یک واحد در صفحهٔ بستن دوره.</summary>
/// <param name="State">وضعیت خود واحد (نبود ردیف = باز).</param>
/// <param name="LockedVia">اگر خودش قفل نیست ولی واحد بالادستی قفل است، کد آن واحد.</param>
public sealed record FsPeriodUnitDto(
    string VahedCode,
    string? VahedName,
    bool IsSelf,
    FsPeriodState State,
    string? LockedVia,
    bool ReopenRequested,
    string? ReopenReason,
    string? ReopenRequestedBy,
    FsUnitRunStatusDto? LatestRun);

/// <summary><paramref name="IsHeadquarters"/> = کاربر از ستاد است (تأیید/رد درخواست بازگشایی).</summary>
public sealed record FsPeriodBoardDto(string Year, bool IsHeadquarters, IReadOnlyList<FsPeriodUnitDto> Units);

public sealed record FsPeriodLogDto(FsPeriodAction Action, FsPeriodState FromState, FsPeriodState ToState, string UserId, string? Reason, DateTime CreatedDate);

/// <summary>V-11 و وضعیت مؤثر: واحد قفل است اگر خودش یا یکی از بالادستی‌هایش قفل باشد.</summary>
public sealed class FsPeriodGuard
{
    private readonly IFsPeriodRepository _periods;
    private readonly IUnitAccessReadRepository _unitAccess;

    public FsPeriodGuard(IFsPeriodRepository periods, IUnitAccessReadRepository unitAccess)
    {
        _periods = periods;
        _unitAccess = unitAccess;
    }

    /// <summary>
    /// واحدهای دامنهٔ یک اجرا که دورهٔ <paramref name="year"/> آن‌ها قفل نیست. دامنه = واحد اجرا، و اگر
    /// <paramref name="includeSubUnits"/>، همهٔ زیرمجموعه‌هایش.
    /// </summary>
    public async Task<IReadOnlyList<string>> GetUnlockedAsync(string vahedCode, bool includeSubUnits, string year, CancellationToken cancellationToken)
    {
        var units = await _unitAccess.GetAllUnitsAsync(cancellationToken);
        var states = (await _periods.GetForYearAsync(year, cancellationToken)).ToDictionary(p => p.VAHEDCODE, p => p.STATE, StringComparer.Ordinal);
        var tree = new UnitTree(units);
        var domain = includeSubUnits ? tree.DescendantsOrSelf(vahedCode) : [vahedCode];

        return domain.Where(u => tree.LockedVia(u, states) is null).OrderBy(u => u, StringComparer.Ordinal).ToList();
    }

    internal sealed class UnitTree
    {
        private readonly Dictionary<string, UnitNode> _byCode;
        private readonly Dictionary<Guid, UnitNode> _byId;
        private readonly ILookup<Guid?, UnitNode> _children;

        public UnitTree(IReadOnlyList<UnitNode> units)
        {
            _byCode = units.GroupBy(u => u.VahedCode).ToDictionary(g => g.Key, g => g.First(), StringComparer.Ordinal);
            _byId = units.ToDictionary(u => u.Id);
            _children = units.ToLookup(u => u.ParentId);
        }

        public string? NameOf(string code) => _byCode.GetValueOrDefault(code)?.VahedName;

        public IReadOnlyList<string> ChildrenOf(string code)
            => _byCode.TryGetValue(code, out var node) ? _children[node.Id].Select(c => c.VahedCode).OrderBy(c => c, StringComparer.Ordinal).ToList() : [];

        public IReadOnlyList<string> DescendantsOrSelf(string code)
        {
            var result = new List<string> { code };

            if (!_byCode.TryGetValue(code, out var root))
            {
                return result;
            }

            var queue = new Queue<UnitNode>([root]);
            var seen = new HashSet<Guid> { root.Id };

            while (queue.Count > 0)
            {
                foreach (var child in _children[queue.Dequeue().Id])
                {
                    if (seen.Add(child.Id))
                    {
                        result.Add(child.VahedCode);
                        queue.Enqueue(child);
                    }
                }
            }

            return result;
        }

        /// <summary>کد خود یا نزدیک‌ترین بالادستیِ قفل؛ <c>null</c> = قفل نیست.</summary>
        public string? LockedVia(string code, IReadOnlyDictionary<string, FsPeriodState> states)
        {
            if (states.GetValueOrDefault(code) == FsPeriodState.Locked)
            {
                return code;
            }

            var node = _byCode.GetValueOrDefault(code);
            var guard = 0;

            while (node?.ParentId is { } pid && _byId.TryGetValue(pid, out var parent) && guard++ < 50)
            {
                if (states.GetValueOrDefault(parent.VahedCode) == FsPeriodState.Locked)
                {
                    return parent.VahedCode;
                }

                node = parent;
            }

            return null;
        }
    }
}

/// <summary>
/// <c>GET api/fs/periods?year=</c> — صفحهٔ «بستن دوره»: واحد هدر و زیرواحدهای مستقیمش (برای ستاد = ادارات کل)
/// با وضعیت دوره، درخواست بازگشایی و آخرین اجرای صورت‌ها.
/// </summary>
public sealed record GetFsPeriodBoardQuery(string Year) : IRequest<FsPeriodBoardDto>, IVahedScopedQuery
{
    [JsonIgnore]
    public string VahedCode { get; set; } = string.Empty;
}

/// <summary><c>GET api/fs/periods/{unit}/log?year=</c> — تاریخچهٔ دورهٔ یک واحد.</summary>
public sealed record GetFsPeriodLogQuery(string UnitCode, string Year) : IRequest<IReadOnlyList<FsPeriodLogDto>>, IVahedScopedQuery
{
    [JsonIgnore]
    public string VahedCode { get; set; } = string.Empty;
}

/// <summary>
/// <c>POST api/fs/periods/{unit}/transitions</c> — اقدام روی دورهٔ واحدی در دسترس:
/// <list type="bullet">
/// <item>بستن: باز ⇒ بستهٔ موقت؛ قفل: بستهٔ موقت ⇒ قفل؛ بازگشایی: بستهٔ موقت ⇒ باز.</item>
/// <item>درخواست بازگشایی (قفل، با دلیل)؛ تأیید آن (فقط ستاد، نه خود درخواست‌کننده) ⇒ بستهٔ موقت؛ رد (فقط ستاد).</item>
/// </list>
/// </summary>
[FsRequires(FsOperation.ClosePeriod)]
public sealed record TransitionFsPeriodCommand(string UnitCode, string Year, FsPeriodAction Action, string? Reason)
    : IRequest<FsPeriodState>, IVahedScopedCommand
{
    [JsonIgnore]
    public string VahedCode { get; set; } = string.Empty;
}

public sealed class TransitionFsPeriodCommandValidator : AbstractValidator<TransitionFsPeriodCommand>
{
    public TransitionFsPeriodCommandValidator()
    {
        RuleFor(x => x.UnitCode).NotEmpty().MaximumLength(4);
        RuleFor(x => x.Year).Matches("^1[34][0-9]{2}$").WithMessage("سال مالی چهاررقمی شمسی.");
        RuleFor(x => x.Action).IsInEnum();
        RuleFor(x => x.Reason).MaximumLength(1000);
        RuleFor(x => x.Reason)
            .NotEmpty()
            .When(x => x.Action is FsPeriodAction.RequestReopen or FsPeriodAction.Reopen or FsPeriodAction.RejectReopen)
            .WithMessage("دلیل لازم است.");
    }
}

public sealed class FsPeriodHandlers :
    IRequestHandler<GetFsPeriodBoardQuery, FsPeriodBoardDto>,
    IRequestHandler<GetFsPeriodLogQuery, IReadOnlyList<FsPeriodLogDto>>,
    IRequestHandler<TransitionFsPeriodCommand, FsPeriodState>
{
    private readonly IFsPeriodRepository _periods;
    private readonly IFsRunRepository _runs;
    private readonly IUnitAccessReadRepository _unitAccess;
    private readonly IFsUnitScopeProvider _scopes;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUser _currentUser;

    public FsPeriodHandlers(
        IFsPeriodRepository periods,
        IFsRunRepository runs,
        IUnitAccessReadRepository unitAccess,
        IFsUnitScopeProvider scopes,
        IUnitOfWork unitOfWork,
        ICurrentUser currentUser)
    {
        _periods = periods;
        _runs = runs;
        _unitAccess = unitAccess;
        _scopes = scopes;
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
    }

    public async Task<FsPeriodBoardDto> Handle(GetFsPeriodBoardQuery request, CancellationToken cancellationToken)
    {
        var scope = await _scopes.GetAsync(request.VahedCode, cancellationToken);
        var tree = new FsPeriodGuard.UnitTree(await _unitAccess.GetAllUnitsAsync(cancellationToken));
        var periods = (await _periods.GetForYearAsync(request.Year, cancellationToken)).ToDictionary(p => p.VAHEDCODE, StringComparer.Ordinal);
        var states = periods.ToDictionary(p => p.Key, p => p.Value.STATE, StringComparer.Ordinal);

        var codes = new List<string> { request.VahedCode };
        codes.AddRange(tree.ChildrenOf(request.VahedCode).Where(scope.Accessible.Contains));
        var runs = (await _runs.GetLatestRunsAsync(codes, request.Year, cancellationToken)).ToDictionary(r => r.VahedCode, StringComparer.Ordinal);

        var units = codes.Select(code =>
        {
            var p = periods.GetValueOrDefault(code);
            var via = tree.LockedVia(code, states);
            return new FsPeriodUnitDto(
                code,
                tree.NameOf(code) ?? scope.Names.GetValueOrDefault(code),
                code == request.VahedCode,
                p?.STATE ?? FsPeriodState.Open,
                via is not null && via != code ? via : null,
                p?.REOPEN_REQUESTED_BY is not null,
                p?.REOPEN_REASON,
                p?.REOPEN_REQUESTED_BY,
                runs.GetValueOrDefault(code));
        }).ToList();

        return new FsPeriodBoardDto(request.Year, scope.IsHeadquarters, units);
    }

    public async Task<IReadOnlyList<FsPeriodLogDto>> Handle(GetFsPeriodLogQuery request, CancellationToken cancellationToken)
    {
        var scope = await _scopes.GetAsync(request.VahedCode, cancellationToken);

        if (!scope.Accessible.Contains(request.UnitCode))
        {
            throw new FsAccessDeniedException("این واحد در دسترس واحد جاری نیست.");
        }

        return (await _periods.GetLogsAsync(request.UnitCode, request.Year, cancellationToken))
            .Select(l => new FsPeriodLogDto(l.ACTION, l.FROM_STATE, l.TO_STATE, l.USERID, l.REASON, l.CREATEDDATE))
            .ToList();
    }

    public async Task<FsPeriodState> Handle(TransitionFsPeriodCommand request, CancellationToken cancellationToken)
    {
        var scope = await _scopes.GetAsync(request.VahedCode, cancellationToken);

        if (!scope.Accessible.Contains(request.UnitCode))
        {
            throw new FsAccessDeniedException("این واحد در دسترس واحد جاری نیست.");
        }

        var user = _currentUser.UserId;
        var now = DateTime.UtcNow;
        var period = await _periods.GetForUpdateAsync(request.UnitCode, request.Year, cancellationToken);

        if (period is null)
        {
            period = new TB_FS_PERIOD
            {
                ID = Guid.NewGuid(),
                VAHEDCODE = request.UnitCode,
                YEAR = request.Year,
                STATE = FsPeriodState.Open,
                CREATEDDATE = now,
                ADDUSERID = user,
            };
            await _periods.AddAsync(period, cancellationToken);
        }

        var from = period.STATE;
        var reason = string.IsNullOrWhiteSpace(request.Reason) ? null : request.Reason.Trim();

        switch (request.Action)
        {
            case FsPeriodAction.Close:
                Require(from, FsPeriodState.Open);
                period.STATE = FsPeriodState.SoftClosed;
                break;

            case FsPeriodAction.Lock:
                Require(from, FsPeriodState.SoftClosed);
                period.STATE = FsPeriodState.Locked;
                break;

            case FsPeriodAction.Reopen:
                Require(from, FsPeriodState.SoftClosed);
                period.STATE = FsPeriodState.Open;
                break;

            case FsPeriodAction.RequestReopen:
                Require(from, FsPeriodState.Locked);

                if (period.REOPEN_REQUESTED_BY is not null)
                {
                    throw new FsTemplateConflictException("برای این دوره درخواست بازگشایی باز وجود دارد.");
                }

                period.REOPEN_REASON = reason;
                period.REOPEN_REQUESTED_BY = user;
                period.REOPEN_REQUESTED_DATE = now;
                break;

            case FsPeriodAction.ApproveReopen:
            case FsPeriodAction.RejectReopen:
                Require(from, FsPeriodState.Locked);

                if (period.REOPEN_REQUESTED_BY is null)
                {
                    throw new FsTemplateConflictException("درخواست بازگشایی‌ای برای این دوره ثبت نشده است.");
                }

                if (!scope.IsHeadquarters)
                {
                    throw new FsAccessDeniedException("تأیید یا رد درخواست بازگشایی فقط با ستاد است.");
                }

                if (request.Action == FsPeriodAction.ApproveReopen && period.REOPEN_REQUESTED_BY == user)
                {
                    throw new FsAccessDeniedException("درخواست‌کنندهٔ بازگشایی نمی‌تواند آن را تأیید کند.");
                }

                if (request.Action == FsPeriodAction.ApproveReopen)
                {
                    period.STATE = FsPeriodState.SoftClosed;
                    reason ??= period.REOPEN_REASON;
                }

                period.REOPEN_REASON = null;
                period.REOPEN_REQUESTED_BY = null;
                period.REOPEN_REQUESTED_DATE = null;
                break;

            default:
                throw new FsTemplateConflictException("اقدام نامعتبر است.");
        }

        period.CHANGEUSERID = user;
        period.UPDATEDDATE = now;

        await _periods.AddLogAsync(new TB_FS_PERIOD_LOG
        {
            ID = Guid.NewGuid(),
            PERIOD_ID = period.ID,
            VAHEDCODE = period.VAHEDCODE,
            YEAR = period.YEAR,
            ACTION = request.Action,
            FROM_STATE = from,
            TO_STATE = period.STATE,
            USERID = user,
            REASON = reason,
            CREATEDDATE = now,
        }, cancellationToken);

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return period.STATE;
    }

    private static void Require(FsPeriodState actual, FsPeriodState required)
    {
        if (actual != required)
        {
            throw new FsTemplateConflictException($"دوره در وضعیت «{Label(actual)}» است و این اقدام روی آن ممکن نیست.");
        }
    }

    internal static string Label(FsPeriodState state) => state switch
    {
        FsPeriodState.Open => "باز",
        FsPeriodState.SoftClosed => "بستهٔ موقت",
        FsPeriodState.Locked => "قفل",
        _ => "نامشخص",
    };
}
