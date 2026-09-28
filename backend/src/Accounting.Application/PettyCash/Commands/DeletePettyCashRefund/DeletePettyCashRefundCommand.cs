using System.Text.Json.Serialization;
using Accounting.Application.Common.Security;
using MediatR;

namespace Accounting.Application.PettyCash.Commands.DeletePettyCashRefund;

/// <summary>
/// <c>POST api/petty-cash/refunds/{id}/delete</c> — soft-deletes a استرداد وجه. Same
/// <c>TB_PC_FUND.REFUND_RECORDER</c> role check as create (بخش ۳-الف،
/// <c>docs/tankhah-khazaneh-module.md</c>). بخش ۳-ب closes the TODO this doc used to flag here:
/// the handler now also blocks the delete (409) when the refund's own date falls inside an
/// already-<see cref="Accounting.Domain.ValueObjects.PettyCashSettlementState.Final"/> settlement
/// period of its fund (<c>PettyCashRefundLockedBySettledPeriodException</c>).
/// </summary>
public sealed record DeletePettyCashRefundCommand(Guid Id) : IRequest, IVahedScopedCommand
{
    [JsonIgnore]
    public string VahedCode { get; set; } = string.Empty;
}
