using System.Text.Json.Serialization;
using Accounting.Application.AccountCodes.Queries.GetTafsiliLevelItems;
using Accounting.Application.Common;
using Accounting.Application.Common.Security;
using MediatR;

namespace Accounting.Application.Treasury.Queries.GetBeneficiaryTafsilis;

/// <summary>
/// <c>GET api/treasury/beneficiary-tafsilis?search=&amp;pageNumber=&amp;pageSize=</c> — اصلاح
/// ۴-الف (۲۰۲۶-۰۹-۲۹، صاحب پروژه؛ <c>docs/tankhah-khazaneh-module.md</c> §۱۰). Returns a page of
/// تفصیلی belonging to the caller's unit's configured
/// <c>TB_TR_SETTING.BENEFICIARY_TAFSIL_GROUP_ID</c> — an EMPTY page (never an error) when the unit
/// has not configured a group yet, so the UI can render "no group configured" from
/// <c>totalCount == 0</c> rather than handling a special error shape.
/// </summary>
/// <param name="Search">Optional free-text filter — see <c>ITreasuryBeneficiaryTafsiliReadRepository.GetPagedAsync</c> XML doc for the digit-vs-name heuristic.</param>
/// <param name="PageNumber">1-based page number.</param>
/// <param name="PageSize">Page size, capped by <see cref="GetBeneficiaryTafsilisQueryValidator.MaxPageSize"/>.</param>
public sealed record GetBeneficiaryTafsilisQuery(
    string? Search,
    int PageNumber,
    int PageSize) : IRequest<PagedResult<TafsiliLookupItemDto>>, IVahedScopedQuery
{
    [JsonIgnore]
    public string VahedCode { get; set; } = string.Empty;
}
