using System.Text.Json.Serialization;
using Accounting.Application.Common.Security;
using MediatR;

namespace Accounting.Application.Treasury.Commands.ApproveTransfer;

/// <summary>
/// <c>POST api/treasury/transfers/{id}/approve</c> — issues the automatic «انتقال» GL voucher and
/// moves the transfer to <see cref="Accounting.Domain.ValueObjects.TransferState.Executed"/>. Only
/// <see cref="Accounting.Domain.ValueObjects.TreasuryRole.Treasurer"/>, ≠ creator, only from
/// <see cref="Accounting.Domain.ValueObjects.TransferState.PendingTreasurer"/>, after two blocking
/// checks (موجودی مبدأ، سقف روزانه). <see cref="BankReference"/> is mandatory here — owner decision
/// ۲۰۲۶-۰۹-۲۹: set at approval time, not before.
/// </summary>
public sealed record ApproveTransferCommand(Guid Id, string BankReference) : IRequest, IVahedScopedCommand
{
    [JsonIgnore]
    public string VahedCode { get; set; } = string.Empty;
}
