using System.Text.Json.Serialization;
using Accounting.Application.Common.Security;
using Accounting.Domain.ValueObjects;
using MediatR;

namespace Accounting.Application.PettyCash.Commands.UpsertPettyCashFundSetting;

/// <summary>
/// <c>POST api/petty-cash/funds/{fundId}/settings</c> — creates the fund's
/// <c>TB_PC_FUND_SETTING</c> row if none exists yet, or fully replaces the writable fields of the
/// existing one (PUT semantics under a POST route, same replace-vs-patch rationale as every other
/// Update command in this project). <see cref="FundId"/> is bound from the route, never the body.
///
/// Named <c>Upsert</c>, not <c>Create</c>/<c>Update</c>, because the caller genuinely does not
/// know (and should not have to know) which of the two this call performs — see
/// <see cref="UpsertPettyCashFundSettingCommandHandler"/>.
/// </summary>
public sealed record UpsertPettyCashFundSettingCommand(
    Guid FundId,
    string? CustodianUserId,
    string? CustodianName,
    decimal? PerDocLimit,
    int? AlertThresholdPercent,
    PettyCashSettlementPeriod? SettlementPeriod) : IRequest, IVahedScopedCommand
{
    /// <summary>
    /// Server-assigned by <c>VahedScopeBehavior</c> — never client input. Used only to verify the
    /// caller owns <see cref="FundId"/>, exactly like every other write handler that touches a
    /// row via its parent's ownership (see <c>CreateVoucherDetailCommandHandler</c> for the same
    /// shape). This table's own <c>VAHEDCODE</c> is stamped from this value on create, never
    /// re-validated as an independent ownership boundary on update — the fund's ownership is the
    /// one that matters.
    /// </summary>
    [JsonIgnore]
    public string VahedCode { get; set; } = string.Empty;
}
