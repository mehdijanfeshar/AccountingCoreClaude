using System.Text.Json.Serialization;
using Accounting.Application.Common;
using Accounting.Application.Common.Security;
using MediatR;

namespace Accounting.Application.Vouchers.Queries.GetVoucherDetails;

/// <summary>
/// Returns a page of <c>TB_VOUCHERSDETAIL</c> rows projected to <see cref="VoucherDetailDto"/>.
/// Only non-deleted rows (<c>ISDELETED != true</c>) are included. <see cref="VoucherHeadId"/> and
/// <see cref="Year"/> are optional filters — when supplied, only matching rows are returned.
/// <see cref="VoucherHeadId"/> is the primary way callers discover the detail lines created
/// together with a head via
/// <see cref="Accounting.Application.Vouchers.Commands.CreateVoucherHead.CreateVoucherHeadCommand.InitialDetails"/>
/// (that command returns only the head's <c>Guid</c>). <see cref="VahedCode"/> is NOT an optional
/// filter — via <see cref="IVahedScopedQuery"/>, every call is unconditionally scoped to the
/// caller's own organizational unit (<c>VAHEDCODE == VahedCode</c>, never
/// <c>OR VAHEDCODE IS NULL</c>; see <c>VoucherDetailReadRepository.GetPagedAsync</c> for why the
/// filter is unconditional). This closes IDOR risk #1 (CLAUDE.md) on the previously-open "list
/// every unit's voucher detail lines" hole.
/// </summary>
/// <param name="PageNumber">1-based page number.</param>
/// <param name="PageSize">Page size, capped by <see cref="GetVoucherDetailsQueryValidator.MaxPageSize"/>.</param>
/// <param name="VoucherHeadId">Optional exact-match filter on the VOUCHERSHEAD_ID column.</param>
/// <param name="Year">Optional exact-match filter on the YEAR column.</param>
public sealed record GetVoucherDetailsQuery(
    int PageNumber,
    int PageSize,
    Guid? VoucherHeadId,
    string? Year) : IRequest<PagedResult<VoucherDetailDto>>, IVahedScopedQuery
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
