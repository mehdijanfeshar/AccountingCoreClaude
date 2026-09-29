using System.Text.Json.Serialization;
using Accounting.Application.AccountCodes.Queries.GetTafsiliLevelItems;
using Accounting.Application.Common;
using Accounting.Application.Common.Security;
using MediatR;

namespace Accounting.Application.Treasury.Queries.GetCustomerTafsilis;

/// <summary>
/// <c>GET api/treasury/customer-tafsilis?search=&amp;pageNumber=&amp;pageSize=</c> — خزانه‌داری،
/// بخش ۴-ج. Same shape as <c>GetBeneficiaryTafsilisQuery</c> (اصلاح ۴-الف) but scoped to the unit's
/// <c>TB_TR_SETTING.CUSTOMER_TAFSIL_GROUP_ID</c> instead of <c>BENEFICIARY_TAFSIL_GROUP_ID</c> —
/// reuses the exact same <c>ITreasuryBeneficiaryTafsiliReadRepository.GetPagedAsync</c> (already
/// generic over any تفصیلی گروه id), not a copy. Returns an EMPTY page (never an error) when the
/// unit has not configured a customer group yet.
/// </summary>
public sealed record GetCustomerTafsilisQuery(
    string? Search,
    int PageNumber,
    int PageSize) : IRequest<PagedResult<TafsiliLookupItemDto>>, IVahedScopedQuery
{
    [JsonIgnore]
    public string VahedCode { get; set; } = string.Empty;
}
