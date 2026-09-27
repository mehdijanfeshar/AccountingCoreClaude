namespace Accounting.Domain.ValueObjects;

/// <summary>
/// نوع مدرک ضمیمهٔ صورت‌هزینهٔ تنخواه (<c>TB_PC_EXPENSE_DOC.EVIDENCE_TYPE</c>) — مقادیر عیناً از
/// <c>docs/tankhah-khazaneh-module.md</c> §۳.
/// </summary>
public enum PettyCashEvidenceType
{
    /// <summary>فاکتور رسمی.</summary>
    OfficialInvoice = 1,

    /// <summary>رسید.</summary>
    Receipt = 2,

    /// <summary>سایر.</summary>
    Other = 3,
}
