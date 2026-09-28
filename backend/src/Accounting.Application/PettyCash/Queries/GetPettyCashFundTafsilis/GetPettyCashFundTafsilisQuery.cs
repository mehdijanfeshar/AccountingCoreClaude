using System.Text.Json.Serialization;
using Accounting.Application.Common.Security;
using Accounting.Application.PettyCash.Queries;
using MediatR;

namespace Accounting.Application.PettyCash.Queries.GetPettyCashFundTafsilis;

/// <summary>
/// <c>GET api/petty-cash/funds/{fundId}/tafsilis</c> — بخش ۳-ب
/// (<c>docs/tankhah-khazaneh-module.md</c> section 9). Every non-deleted
/// <c>TB_PC_FUND_LINK_TAFSILI</c> row of the fund. Deliberately does not return 404 for a
/// nonexistent fund — mirrors <c>GetPettyCashRefundsQuery</c>'s "bare list, always 200" shape;
/// an unknown/foreign fund id simply yields an empty list (unit-scoped, so a foreign fund's rows
/// are never visible regardless).
/// </summary>
public sealed record GetPettyCashFundTafsilisQuery(Guid FundId) : IRequest<IReadOnlyList<PettyCashSettlementTafsiliDto>>, IVahedScopedQuery
{
    [JsonIgnore]
    public string VahedCode { get; set; } = string.Empty;
}
