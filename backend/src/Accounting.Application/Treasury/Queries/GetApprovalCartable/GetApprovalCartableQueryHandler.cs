using Accounting.Application.Common;
using Accounting.Application.Common.Exceptions;
using Accounting.Application.Common.Interfaces;
using Accounting.Application.Common.Security;
using Accounting.Application.PettyCash.Queries;
using Accounting.Application.Treasury.Commands.Common;
using Accounting.Domain.ValueObjects;
using MediatR;

namespace Accounting.Application.Treasury.Queries.GetApprovalCartable;

/// <summary>
/// Merges two independent, flat sources into one کارتابل, in C# (never a cross-module SQL join —
/// <c>docs/tankhah-khazaneh-module.md</c> §۱۰): (۱) درخواست‌های پرداخت currently
/// PendingUnitManager/PendingFinanceManager/PendingCeo (<see cref="IPaymentRequestReadRepository.GetPendingForCartableAsync"/>),
/// (۲) تنخواه ترمیم‌های currently <see cref="Domain.ValueObjects.PettyCashReplenishmentState.PendingTreasurer"/>
/// from the existing <see cref="IPettyCashReplenishmentReadRepository"/> (نمایشی‌فقط — این ماژول
/// چیزی به تنخواه اضافه/تغییر نمی‌دهد). Sorted by age (oldest first), then paged in memory —
/// acceptable for a کارتابل-sized list (never large enough to warrant a shared, cross-module view).
/// </summary>
public sealed class GetApprovalCartableQueryHandler : IRequestHandler<GetApprovalCartableQuery, PagedResult<ApprovalCartableItemDto>>
{
    private readonly IPaymentRequestReadRepository _paymentRequestReadRepository;
    private readonly IPettyCashReplenishmentReadRepository _replenishmentReadRepository;
    private readonly IPettyCashFundReviewerReadRepository _fundReviewerReadRepository;
    private readonly ITreasuryRoleRepository _treasuryRoleRepository;
    private readonly ICurrentUser _currentUser;

    /// <summary>
    /// A کارتابل is, by nature, a small "things waiting for a human" list — this cap keeps the
    /// ترمیم half of the merge a single bounded query instead of an unbounded one, without
    /// introducing a second, dedicated "get all" read method on a petty-cash interface this module
    /// does not own (see class XML doc).
    /// </summary>
    private const int ReplenishmentFetchCap = 500;

    public GetApprovalCartableQueryHandler(
        IPaymentRequestReadRepository paymentRequestReadRepository,
        IPettyCashReplenishmentReadRepository replenishmentReadRepository,
        IPettyCashFundReviewerReadRepository fundReviewerReadRepository,
        ITreasuryRoleRepository treasuryRoleRepository,
        ICurrentUser currentUser)
    {
        _paymentRequestReadRepository = paymentRequestReadRepository;
        _replenishmentReadRepository = replenishmentReadRepository;
        _fundReviewerReadRepository = fundReviewerReadRepository;
        _treasuryRoleRepository = treasuryRoleRepository;
        _currentUser = currentUser;
    }

    public async Task<PagedResult<ApprovalCartableItemDto>> Handle(
        GetApprovalCartableQuery request, CancellationToken cancellationToken)
    {
        var userId = _currentUser.UserId;
        var now = DateTime.UtcNow;

        var paymentRequests = await _paymentRequestReadRepository.GetPendingForCartableAsync(request.VahedCode, cancellationToken);

        var callerTreasuryRoles = await _treasuryRoleRepository.GetActiveRolesAsync(request.VahedCode, userId, cancellationToken);

        var items = new List<ApprovalCartableItemDto>();

        foreach (var pr in paymentRequests)
        {
            var requiredRole = PaymentRequestStageRoleMap.RequiredRoleForState(pr.RequestState);

            var pendingForMe = requiredRole.HasValue
                && callerTreasuryRoles.Contains(requiredRole.Value)
                && !string.Equals(pr.AddUserId, userId, StringComparison.Ordinal);

            var ageDays = (int)(now - (pr.SubmittedDate ?? pr.CreatedDate)).TotalDays;

            items.Add(new ApprovalCartableItemDto(
                "payment",
                pr.Id,
                pr.Code,
                pr.BeneficiaryName,
                null,
                pr.NetPayableAmount,
                (int)pr.RequestState,
                PaymentRequestStateConflictException.StateLabel(pr.RequestState),
                ageDays,
                pr.DueDate,
                pendingForMe));
        }

        var replenishmentPage = await _replenishmentReadRepository.GetPagedAsync(
            pageNumber: 1,
            pageSize: ReplenishmentFetchCap,
            fundId: null,
            state: PettyCashReplenishmentState.PendingTreasurer,
            vahedCode: request.VahedCode,
            cancellationToken: cancellationToken);

        // یک کوئری بررسی‌کنندگان به‌ازای هر تنخواهِ متمایز — کارتابل کوچک است، N+1 اینجا توجیه‌پذیر
        // است در برابر افزودن یک متد read جدید به رابط تنخواه که این ماژول مالکش نیست.
        var reviewerCacheByFund = new Dictionary<Guid, IReadOnlyList<PettyCashFundReviewerDto>>();

        foreach (var rep in replenishmentPage.Items)
        {
            if (!reviewerCacheByFund.TryGetValue(rep.FundId, out var reviewers))
            {
                reviewers = await _fundReviewerReadRepository.GetByFundIdAsync(rep.FundId, request.VahedCode, cancellationToken);
                reviewerCacheByFund[rep.FundId] = reviewers;
            }

            var pendingForMe = reviewers.Any(r =>
                r.Role == PettyCashRole.Treasurer && string.Equals(r.ReviewerUserId, userId, StringComparison.Ordinal))
                && !string.Equals(rep.AddUserId, userId, StringComparison.Ordinal);

            var ageDays = (int)(now - rep.CreatedDate).TotalDays;

            items.Add(new ApprovalCartableItemDto(
                "replenishment",
                rep.Id,
                rep.Code,
                rep.FundName,
                null,
                rep.TotalAmount,
                (int)rep.State,
                ReplenishmentStateLabel(rep.State),
                ageDays,
                null,
                pendingForMe));
        }

        var ordered = items.OrderByDescending(i => i.AgeDays).ToList();

        var page = ordered
            .Skip((request.PageNumber - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToList();

        return new PagedResult<ApprovalCartableItemDto>
        {
            Items = page,
            PageNumber = request.PageNumber,
            PageSize = request.PageSize,
            TotalCount = ordered.Count,
        };
    }

    private static string ReplenishmentStateLabel(PettyCashReplenishmentState state) => state switch
    {
        PettyCashReplenishmentState.Draft => "پیش‌نویس",
        PettyCashReplenishmentState.PendingFinanceManager => "در انتظار تأیید مدیر مالی",
        PettyCashReplenishmentState.PendingTreasurer => "در انتظار اقدام خزانه‌دار",
        PettyCashReplenishmentState.Paid => "پرداخت‌شده",
        PettyCashReplenishmentState.Rejected => "ردشده",
        _ => "نامشخص",
    };
}
