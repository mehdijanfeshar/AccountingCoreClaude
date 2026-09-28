using System.Text.Json.Serialization;
using Accounting.Application.Common;
using Accounting.Application.Common.Security;
using Accounting.Domain.ValueObjects;
using MediatR;

namespace Accounting.Application.PettyCash.Queries.GetPettyCashReplenishments;

/// <summary>
/// <c>GET api/petty-cash/replenishments?fundId=&amp;state=&amp;pageNumber=&amp;pageSize=</c> —
/// بخش ۳-الف (<c>docs/tankhah-khazaneh-module.md</c>).
/// </summary>
public sealed record GetPettyCashReplenishmentsQuery(
    int PageNumber,
    int PageSize,
    Guid? FundId = null,
    PettyCashReplenishmentState? State = null) : IRequest<PagedResult<PettyCashReplenishmentListItemDto>>, IVahedScopedQuery
{
    [JsonIgnore]
    public string VahedCode { get; set; } = string.Empty;
}
