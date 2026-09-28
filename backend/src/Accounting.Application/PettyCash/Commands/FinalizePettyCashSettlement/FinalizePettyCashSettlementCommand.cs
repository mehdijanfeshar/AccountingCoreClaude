using System.Text.Json.Serialization;
using Accounting.Application.Common.Security;
using MediatR;

namespace Accounting.Application.PettyCash.Commands.FinalizePettyCashSettlement;

/// <summary>
/// <c>POST api/petty-cash/funds/{fundId}/settlement/finalize</c> — بخش ۳-ب
/// (<c>docs/tankhah-khazaneh-module.md</c> section 9). Issues the settlement GL voucher and
/// finalizes the fund's current Draft period, all in one transaction
/// (<see cref="FinalizePettyCashSettlementCommandHandler"/>).
/// </summary>
/// <param name="AcknowledgeInFlightTransfer">Must be <see langword="true"/> when the fund has any
/// document still در جریان (New/PendingReview/Returned) — otherwise
/// <see cref="Accounting.Application.Common.Exceptions.PettyCashSettlementInFlightAcknowledgeRequiredException"/>.</param>
public sealed record FinalizePettyCashSettlementCommand(
    Guid FundId, bool AcknowledgeInFlightTransfer) : IRequest<FinalizePettyCashSettlementResult>, IVahedScopedCommand
{
    [JsonIgnore]
    public string VahedCode { get; set; } = string.Empty;
}

public sealed record FinalizePettyCashSettlementResult(
    Guid PeriodId, Guid VoucherHeadId, string VoucherDocNum, int SettledDocCount);
