using System.Text.Json.Serialization;
using Accounting.Application.Common.Security;
using MediatR;

namespace Accounting.Application.PettyCash.Queries.GetPettyCashFundSetting;

/// <summary>
/// <c>GET api/petty-cash/funds/{fundId}/settings</c>. Per the frontend contract, a fund that
/// exists but has no <c>TB_PC_FUND_SETTING</c> row yet is reported as <b>404</b> ("not
/// configured"), the same status a missing fund gets — the caller does not need to tell the two
/// apart, since both mean "there is nothing to show on the settings form yet, use the upsert
/// endpoint". See <see cref="GetPettyCashFundSettingQueryHandler"/>.
/// </summary>
public sealed record GetPettyCashFundSettingQuery(Guid FundId) : IRequest<PettyCashFundSettingDto>, IVahedScopedQuery
{
    [JsonIgnore]
    public string VahedCode { get; set; } = string.Empty;
}
