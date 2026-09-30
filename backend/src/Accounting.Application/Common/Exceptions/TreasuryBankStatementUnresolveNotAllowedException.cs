namespace Accounting.Application.Common.Exceptions;

/// <summary>
/// Thrown by <c>unresolve</c> (<c>POST statements/{id}/lines/{lineId}/unresolve</c>) when the
/// line's <c>RESOLUTION_TYPE = BankFeeVoucher</c> resolution voucher is no longer
/// <c>DocLife.Temporary</c> (someone finalised it outside this flow) — خزانه‌داری، بخش ۴-د
/// (<c>docs/tankhah-khazaneh-module.md</c> §۱۰). Owner instruction: if safe automatic soft-delete
/// is not possible, disallow rather than silently leave a dangling reference. <b>409.</b>
/// </summary>
public sealed class TreasuryBankStatementUnresolveNotAllowedException : Exception
{
    public TreasuryBankStatementUnresolveNotAllowedException(Guid lineId)
        : base($"BankStatementLine {lineId} cannot be unresolved — its resolution voucher is no longer temporary.")
    {
        LineId = lineId;
    }

    public Guid LineId { get; }

    public string PublicDetail =>
        "سند کارمزد بانکی این ردیف دیگر موقت نیست (نهایی یا حذف شده) — لغو حل این ردیف ممکن نیست.";
}
