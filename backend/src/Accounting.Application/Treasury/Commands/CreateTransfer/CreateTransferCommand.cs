using System.Text.Json.Serialization;
using Accounting.Application.Common.Security;
using Accounting.Domain.ValueObjects;
using MediatR;

namespace Accounting.Application.Treasury.Commands.CreateTransfer;

/// <summary><c>POST api/treasury/transfers</c> — creates an انتقال وجه as پیش‌نویس.</summary>
public sealed record CreateTransferCommand(
    Guid SourceBankAccountId,
    Guid DestBankAccountId,
    decimal Amount,
    string TransferDate,
    TreasuryPaymentMethod TransferMethod,
    string Reason,
    string Year) : IRequest<Guid>, IVahedScopedCommand
{
    [JsonIgnore]
    public string VahedCode { get; set; } = string.Empty;
}
