using System.Text.Json.Serialization;
using Accounting.Application.Common;
using Accounting.Application.Common.Security;
using Accounting.Application.Treasury.Queries;
using MediatR;

namespace Accounting.Application.Treasury.Queries.GetApprovalCartable;

/// <summary>
/// <c>GET api/treasury/approval-cartable?pageNumber=&amp;pageSize=</c> — merges Pending* درخواست
/// پرداخت rows with <c>PendingTreasurer</c> تنخواه ترمیم rows into one list, sorted by age
/// (خزانه‌داری، بخش ۴-الف؛ <c>docs/tankhah-khazaneh-module.md</c> §۱۰). See
/// <see cref="GetApprovalCartableQueryHandler"/> XML doc for why this is two flat queries merged in
/// C#, never a cross-module SQL join.
/// </summary>
public sealed record GetApprovalCartableQuery(int PageNumber, int PageSize)
    : IRequest<PagedResult<ApprovalCartableItemDto>>, IVahedScopedQuery
{
    [JsonIgnore]
    public string VahedCode { get; set; } = string.Empty;
}
