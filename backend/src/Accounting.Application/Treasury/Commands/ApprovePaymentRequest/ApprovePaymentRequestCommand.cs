using System.Text.Json.Serialization;
using Accounting.Application.Common.Security;
using MediatR;

namespace Accounting.Application.Treasury.Commands.ApprovePaymentRequest;

/// <summary>
/// <c>POST api/treasury/payment-requests/{id}/approve</c> — advances a درخواست پرداخت one stage
/// through its approval chain. Caller must hold the role matching the request's current stage
/// (UnitManager/FinanceManager/Ceo) for the unit, and must not be its own creator nor the approver
/// of its immediately-preceding stage (خزانه‌داری، بخش ۴-الف؛
/// <c>docs/tankhah-khazaneh-module.md</c> §۱۰).
/// </summary>
public sealed record ApprovePaymentRequestCommand(Guid Id, string? Note) : IRequest, IVahedScopedCommand
{
    [JsonIgnore]
    public string VahedCode { get; set; } = string.Empty;
}
