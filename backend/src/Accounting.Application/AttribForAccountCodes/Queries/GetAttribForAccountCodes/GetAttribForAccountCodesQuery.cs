using System.Text.Json.Serialization;
using Accounting.Application.Common;
using Accounting.Application.Common.Security;
using Accounting.Domain.ValueObjects;
using MediatR;

namespace Accounting.Application.AttribForAccountCodes.Queries.GetAttribForAccountCodes;

/// <summary>
/// Returns a page of <c>TB_ATTRIBFORACCOUNTCODE</c> rows projected to
/// <see cref="AttribForAccountCodeDto"/>. Only non-deleted rows (<c>ISDELETED != true</c>) are
/// included, and — via <see cref="IVahedScopedQuery"/> — only rows belonging to the caller's own
/// organizational unit (<c>VAHEDCODE == VahedCode</c>, never <c>OR VAHEDCODE IS NULL</c>; see
/// <c>AttribForAccountCodeReadRepository.GetPagedAsync</c> for why the filter is unconditional).
/// </summary>
/// <param name="PageNumber">1-based page number.</param>
/// <param name="PageSize">Page size, capped by <see cref="GetAttribForAccountCodesQueryValidator.MaxPageSize"/>.</param>
/// <remarks>
/// Filter parameters are kept flat here (rather than as a nested
/// <see cref="AttribForAccountCodeFilter"/>) to match <c>GetVoucherHeadsQuery</c>: the grouped
/// record is the <i>repository</i> boundary type, built by the handler, which keeps both the
/// FluentValidation rules and the controller's query-string binding straightforward.
/// </remarks>
public sealed record GetAttribForAccountCodesQuery(
    int PageNumber,
    int PageSize,
    string? MoinCodeFrom = null,
    string? MoinCodeTo = null,
    AttribSum? AttribSum = null,
    AttribFlag? Flag = null,
    string? Year = null)
    : IRequest<PagedResult<AttribForAccountCodeDto>>, IVahedScopedQuery
{
    /// <summary>
    /// Organizational unit code to filter by. Server-assigned by <c>VahedScopeBehavior</c> from
    /// the authenticated caller's own <c>VahedCode</c> — never bound from client input.
    /// </summary>
    [JsonIgnore]
    public string VahedCode { get; set; } = string.Empty;
}
