using System.Text.Json.Serialization;
using Accounting.Application.Common;
using Accounting.Application.Common.Security;
using Accounting.Domain.ValueObjects;
using MediatR;

namespace Accounting.Application.Vouchers.Queries.GetVoucherHeads;

/// <summary>
/// Returns a page of <c>TB_VOUCHERSHEAD</c> rows projected to <see cref="VoucherHeadDto"/>.
/// Only non-deleted rows (<c>ISDELETED != true</c>) are included. <see cref="Year"/> is an
/// optional filter — when supplied, only matching rows are returned. <see cref="VahedCode"/> is
/// NOT an optional filter — via <see cref="IVahedScopedQuery"/>, every call is unconditionally
/// scoped to the caller's own organizational unit (<c>VAHEDCODE == VahedCode</c>, never
/// <c>OR VAHEDCODE IS NULL</c>; see <c>VoucherHeadReadRepository.GetPagedAsync</c> for why the
/// filter is unconditional). This closes IDOR risk #1 (CLAUDE.md) on the previously-open
/// "list every unit's vouchers" hole.
/// </summary>
/// <param name="PageNumber">1-based page number.</param>
/// <param name="PageSize">Page size, capped by <see cref="GetVoucherHeadsQueryValidator.MaxPageSize"/>.</param>
/// <param name="Year">Optional exact-match filter on the YEAR column.</param>
/// <param name="DocNumFrom">Optional inclusive lower bound of the DOC_NUM range.</param>
/// <param name="DocNumTo">Optional inclusive upper bound of the DOC_NUM range.</param>
/// <param name="DateDocFrom">Optional inclusive lower bound of the DATE_DOC range (<c>YYYYMMDD</c>).</param>
/// <param name="DateDocTo">Optional inclusive upper bound of the DATE_DOC range (<c>YYYYMMDD</c>).</param>
/// <param name="SystemTypeId">Optional exact-match filter on SYSTEM_TYPE (نوع سند).</param>
/// <param name="DocLife">Optional exact-match filter on DOCLIFE (وضعیت سند) — backs the کارتابل status tabs.</param>
public sealed record GetVoucherHeadsQuery(
    int PageNumber,
    int PageSize,
    string? Year,
    string? DocNumFrom = null,
    string? DocNumTo = null,
    string? DateDocFrom = null,
    string? DateDocTo = null,
    Guid? SystemTypeId = null,
    DocLife? DocLife = null) : IRequest<PagedResult<VoucherHeadDto>>, IVahedScopedQuery
{
    /// <summary>
    /// Organizational unit code to filter by. Server-assigned by <c>VahedScopeBehavior</c> from
    /// the authenticated caller's own <c>VahedCode</c> — never bound from client input (the
    /// controller builds this query from individually-bound query-string parameters, not a
    /// deserialized body, but <see cref="JsonIgnoreAttribute"/> is still applied here for
    /// consistency with every other <see cref="IVahedScopedQuery"/>/<c>IVahedScopedCommand</c>
    /// implementer and to keep it out of the Swagger schema).
    /// </summary>
    [JsonIgnore]
    public string VahedCode { get; set; } = string.Empty;
}
