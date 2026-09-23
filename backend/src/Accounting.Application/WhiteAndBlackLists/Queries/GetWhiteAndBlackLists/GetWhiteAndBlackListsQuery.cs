using Accounting.Application.Common;
using Accounting.Domain.ValueObjects;
using MediatR;

namespace Accounting.Application.WhiteAndBlackLists.Queries.GetWhiteAndBlackLists;

/// <summary>
/// Returns a page of <c>TB_WHITEANDBLACKLIST</c> rows projected to
/// <see cref="WhiteAndBlackListDto"/>. Only non-deleted rows (<c>ISDELETED != true</c>) are
/// included.
///
/// Every filter below is optional and they combine with AND, mirroring the filter panel of the
/// «دسترسی کدینگ حسابداری» screen. The four date filters are <b>range bounds</b>, not equality:
/// the <c>From…</c> ones keep rows whose stored value is <c>&gt;=</c> the argument and the
/// <c>To…</c> ones keep rows whose stored value is <c>&lt;=</c> it — the same four operators the
/// old Angular screen sent as <c>SearchParam.operator</c> 4/5.
///
/// ⚠️ Comparing the Legacy date columns as <b>strings</b> is correct here and is not a shortcut:
/// they are zero-padded <c>YYYYMMDD</c> Jalali text (verified on live data — e.g.
/// <c>"14040101"</c>…<c>"14061229"</c>), a format in which lexicographic order and chronological
/// order are the same. Do not "fix" this by parsing to a date: there is no date type on these
/// columns and parsing would move the comparison out of SQL.
/// </summary>
/// <param name="PageNumber">1-based page number.</param>
/// <param name="PageSize">Page size, capped by <see cref="GetWhiteAndBlackListsQueryValidator.MaxPageSize"/>.</param>
/// <param name="AccountCodeId">Optional: keep only rows for this <c>ACCOUNTCODE_ID</c>.</param>
/// <param name="VahedTypeId">Optional: keep only rows for this <c>VAHEDTYPE_ID</c>.</param>
/// <param name="State">Optional: keep only rows in this <see cref="WhiteBlackListState"/>.</param>
/// <param name="FromAuthorizedDate">Optional lower bound on <c>FROMAUTHORIZEDDATE</c>.</param>
/// <param name="ToAuthorizedDate">Optional upper bound on <c>TOAUTHORIZEDDATE</c>.</param>
/// <param name="FromLimitationDate">Optional lower bound on <c>FROMLIMITATIONDATE</c>.</param>
/// <param name="ToLimitationDate">Optional upper bound on <c>TOLIMITATIONDATE</c>.</param>
public sealed record GetWhiteAndBlackListsQuery(
    int PageNumber,
    int PageSize,
    Guid? AccountCodeId = null,
    Guid? VahedTypeId = null,
    WhiteBlackListState? State = null,
    string? FromAuthorizedDate = null,
    string? ToAuthorizedDate = null,
    string? FromLimitationDate = null,
    string? ToLimitationDate = null) : IRequest<PagedResult<WhiteAndBlackListDto>>;
