namespace Accounting.Domain.ValueObjects;

/// <summary>نوع ذی‌نفعِ درخواست پرداخت — <c>TB_TR_PAYMENT_REQUEST.PAYMENT_TYPE</c>.</summary>
public enum TreasuryPaymentType
{
    /// <summary>تأمین‌کننده/پیمانکار.</summary>
    SupplierOrContractor = 1,

    /// <summary>کارمند.</summary>
    Employee = 2,

    /// <summary>سایر.</summary>
    Other = 3,
}
