namespace Accounting.Application.Common.Exceptions;

/// <summary>
/// The unit's «حساب بستانکاران» (<c>TB_TR_SETTING.PAYABLES_ACCOUNT_ID</c>) requires exactly one
/// تفصیلی level, but the درخواست پرداخت has no <c>BENEFICIARY_TAFSILI_ID</c> to fill it with —
/// خزانه‌داری، بخش ۴-ب (owner decision ۲۰۲۶-۰۹-۲۹ #۴). <b>409</b>.
/// </summary>
public sealed class PaymentRequestPayablesBeneficiaryRequiredException : Exception
{
    public PaymentRequestPayablesBeneficiaryRequiredException(Guid paymentRequestId)
        : base($"Payment request {paymentRequestId} has no BENEFICIARY_TAFSILI_ID, but the payables account requires one تفصیلی level.")
    {
        PaymentRequestId = paymentRequestId;
    }

    public Guid PaymentRequestId { get; }

    public string PublicDetail => "برای این حساب بستانکاران، تفصیلی ذی‌نفع لازم است.";
}
