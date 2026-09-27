using Accounting.Domain.ValueObjects;

namespace Accounting.Application.PettyCash.Queries;

/// <summary>
/// Optional filters for <c>GetPettyCashExpenseDocs</c>. Every member is optional — a
/// <see langword="null"/>/empty value means "do not filter on this". <see cref="States"/> and
/// <see cref="State"/> both narrow <c>DOC_STATE</c>; when <see cref="States"/> is non-empty it
/// takes precedence over <see cref="State"/> entirely (they are not ANDed together — see
/// <c>PettyCashExpenseDocReadRepository.GetPagedAsync</c>), so a caller who wants the «در جریان»
/// tab (New + PendingReview + Returned combined) can ask for it in one request instead of three.
/// </summary>
/// <param name="FundId">Exact-match filter on REVOLVINGFUND_ID.</param>
/// <param name="State">Exact-match filter on DOC_STATE. Ignored when <see cref="States"/> is non-empty.</param>
/// <param name="States">OR filter on DOC_STATE — matches any of the listed states.</param>
/// <param name="Search">Free-text match against CHARGEANDCOST_CODE, VENDOR_NAME, INVOICE_NO and DESCRIPTION.</param>
public sealed record PettyCashExpenseDocFilter(
    Guid? FundId = null,
    PettyCashDocState? State = null,
    IReadOnlyList<PettyCashDocState>? States = null,
    string? Search = null);
