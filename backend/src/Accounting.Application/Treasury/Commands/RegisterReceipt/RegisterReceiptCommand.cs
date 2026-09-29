using System.Text.Json.Serialization;
using Accounting.Application.Common.Security;
using MediatR;

namespace Accounting.Application.Treasury.Commands.RegisterReceipt;

/// <summary>
/// <c>POST api/treasury/receipts/{id}/register</c> — issues the automatic «دریافت» GL voucher +
/// Legacy <c>TB_PAYRECIVHEAD/DETAIL</c> and moves the receipt to
/// <see cref="Accounting.Domain.ValueObjects.ReceiptState.Registered"/>. Only
/// <see cref="Accounting.Domain.ValueObjects.TreasuryRole.Treasurer"/>, only from
/// <see cref="Accounting.Domain.ValueObjects.ReceiptState.Draft"/>.
/// </summary>
public sealed record RegisterReceiptCommand(Guid Id) : IRequest, IVahedScopedCommand
{
    [JsonIgnore]
    public string VahedCode { get; set; } = string.Empty;
}
