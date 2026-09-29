using System.Text.Json.Serialization;
using Accounting.Application.Common.Security;
using Accounting.Domain.ValueObjects;
using MediatR;

namespace Accounting.Application.Treasury.Commands.UpdatePaymentRequest;

/// <summary>
/// <c>POST api/treasury/payment-requests/{id}/update</c> — fully replaces the editable fields of an
/// existing درخواست پرداخت. Only allowed while it is <see cref="PaymentRequestState.Draft"/>/
/// <see cref="PaymentRequestState.Returned"/>, and only by its own creator. Never changes
/// <c>CODE</c>/<c>VAHEDCODE</c>/<c>YEAR</c>/<c>REQUEST_STATE</c> — use
/// <c>SubmitPaymentRequestCommand</c> for the state transition. <c>Id</c> is taken from the route,
/// never the body.
/// </summary>
public sealed record UpdatePaymentRequestCommand(
    Guid Id,
    string BeneficiaryName,
    string? BeneficiaryNationalId,
    Guid? BeneficiaryTafsiliId,
    TreasuryPaymentType PaymentType,
    string? InvoiceRef,
    bool InvoiceApproved,
    Guid ExpenseAccountId,
    Guid? CostCenterTafsiliId,
    decimal AmountBeforeTax,
    decimal? VatPercent,
    decimal? VatAmount,
    decimal? InsuranceDeductionPercent,
    decimal? InsuranceDeductionAmount,
    string DueDate,
    Guid PaymentAccountId,
    TreasuryPaymentMethod? PaymentMethod,
    string? Description) : IRequest, IVahedScopedCommand
{
    [JsonIgnore]
    public string VahedCode { get; set; } = string.Empty;
}
