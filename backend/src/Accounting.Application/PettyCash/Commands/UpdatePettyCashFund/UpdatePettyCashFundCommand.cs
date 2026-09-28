using System.Text.Json.Serialization;
using Accounting.Application.Common.Security;
using Accounting.Domain.ValueObjects;
using MediatR;

namespace Accounting.Application.PettyCash.Commands.UpdatePettyCashFund;

/// <summary>
/// <c>POST api/petty-cash/funds/{fundId}/update</c> — fully replaces the writable fields of an
/// existing <c>TB_PC_FUND</c> row. <c>Id</c> is bound from the route, never the body. Never
/// changes <c>ID</c>/<c>VAHEDCODE</c> identity — only the fields below.
/// </summary>
public sealed record UpdatePettyCashFundCommand(
    Guid Id,
    string Code,
    string Name,
    string CustodianUserId,
    string? CustodianName,
    decimal Ceiling,
    decimal PerDocLimit,
    decimal FinanceManagerApprovalLimit,
    int? AlertThresholdPercent,
    Guid? AccountCodeId,
    PettyCashSettlementPeriod? SettlementPeriod,
    bool IsActive) : IRequest, IVahedScopedCommand
{
    [JsonIgnore]
    public string VahedCode { get; set; } = string.Empty;
}
