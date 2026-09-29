using System.Text.Json.Serialization;
using Accounting.Application.Common.Security;
using Accounting.Domain.ValueObjects;
using MediatR;

namespace Accounting.Application.Treasury.Commands.CreatePaymentRequest;

/// <summary>
/// <c>POST api/treasury/payment-requests</c> — خزانه‌داری، بخش ۴-الف
/// (<c>docs/tankhah-khazaneh-module.md</c> §۱۰). Creates a درخواست پرداخت as a پیش‌نویس, or — when
/// <see cref="Submit"/> is <see langword="true"/> — submits it straight to
/// <see cref="PaymentRequestState.PendingUnitManager"/>, applying the same rules as
/// <c>SubmitPaymentRequestCommand</c> (same shape as <c>CreatePettyCashExpenseDocCommand.Submit</c>).
///
/// <c>Year</c> exists for the same reason every other composite-create command in this project
/// carries it: <c>TB_TR_PAYMENT_REQUEST.YEAR</c>/the CODE counter are per-(VahedCode, Year), and
/// the server never derives a fiscal year from a free-text date — caller/session supplies it.
/// </summary>
/// <param name="BeneficiaryName">TB_TR_PAYMENT_REQUEST.BENEFICIARY_NAME.</param>
/// <param name="BeneficiaryNationalId">TB_TR_PAYMENT_REQUEST.BENEFICIARY_NATIONAL_ID.</param>
/// <param name="BeneficiaryTafsiliId">TB_TR_PAYMENT_REQUEST.BENEFICIARY_TAFSILI_ID.</param>
/// <param name="PaymentType">TB_TR_PAYMENT_REQUEST.PAYMENT_TYPE.</param>
/// <param name="InvoiceRef">TB_TR_PAYMENT_REQUEST.INVOICE_REF.</param>
/// <param name="InvoiceApproved">دستی — تیک ثبت‌کننده که فاکتور را بررسی کرده است.</param>
/// <param name="ExpenseAccountId">TB_TR_PAYMENT_REQUEST.EXPENSE_ACCOUNT_ID (FK به TB_ACCOUNTCODE — کنترل وجود سمت Application).</param>
/// <param name="CostCenterTafsiliId">TB_TR_PAYMENT_REQUEST.COST_CENTER_TAFSILI_ID.</param>
/// <param name="AmountBeforeTax">TB_TR_PAYMENT_REQUEST.AMOUNT_BEFORE_TAX.</param>
/// <param name="VatPercent">وقتی مقدار دارد، VAT_AMOUNT را سرور از روی آن حساب می‌کند (<see cref="VatAmount"/> نادیده گرفته می‌شود).</param>
/// <param name="VatAmount">فقط وقتی <see cref="VatPercent"/> خالی است استفاده می‌شود — رجوع به <c>PaymentRequestAmountCalculator</c>.</param>
/// <param name="InsuranceDeductionPercent">وقتی مقدار دارد، INSURANCE_DEDUCTION_AMOUNT را سرور از روی آن حساب می‌کند.</param>
/// <param name="InsuranceDeductionAmount">فقط وقتی <see cref="InsuranceDeductionPercent"/> خالی است استفاده می‌شود.</param>
/// <param name="DueDate">TB_TR_PAYMENT_REQUEST.DUE_DATE (شمسی YYYYMMDD).</param>
/// <param name="PaymentAccountId">TB_TR_PAYMENT_REQUEST.PAYMENT_ACCOUNT_ID (FK به TB_ACCOUNT — کنترل وجود سمت Application).</param>
/// <param name="PaymentMethod">TB_TR_PAYMENT_REQUEST.PAYMENT_METHOD.</param>
/// <param name="Description">TB_TR_PAYMENT_REQUEST.DESCRIPTION.</param>
/// <param name="Year">Fiscal year (session-selected).</param>
/// <param name="Submit">وقتی true، بلافاصله به PendingUnitManager می‌رود (قواعد Submit همان‌جا اعمال می‌شود).</param>
public sealed record CreatePaymentRequestCommand(
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
    string? Description,
    string Year,
    bool Submit) : IRequest<Guid>, IVahedScopedCommand
{
    [JsonIgnore]
    public string VahedCode { get; set; } = string.Empty;
}
