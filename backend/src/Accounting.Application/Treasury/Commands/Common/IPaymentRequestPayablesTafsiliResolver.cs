using Accounting.Application.Vouchers.Commands.Common;

namespace Accounting.Application.Treasury.Commands.Common;

/// <summary>
/// Resolves the تفصیلی assignment(s) for the two account "shapes" بخش ۴-ب's automatic vouchers
/// need — خزانه‌داری، بخش ۴-ب (owner decision ۲۰۲۶-۰۹-۲۹ #۴، <c>docs/tankhah-khazaneh-module.md</c>
/// §۱۰). Shared by <see cref="IPaymentRequestLiabilityVoucherBuilder"/> (voucher 1's «حساب
/// بستانکاران» credit line) and <see cref="IPaymentRequestPaymentVoucherBuilder"/> (voucher 2's
/// «حساب بستانکاران» debit line) so the two vouchers can never disagree about the same request's
/// same بستانکاران line.
/// </summary>
public interface IPaymentRequestPayablesTafsiliResolver
{
    /// <summary>
    /// «حساب بستانکاران» — 0 configured levels ⇒ no link; exactly 1 ⇒ the request's
    /// <c>BENEFICIARY_TAFSILI_ID</c> at that level (missing ⇒
    /// <see cref="Accounting.Application.Common.Exceptions.PaymentRequestPayablesBeneficiaryRequiredException"/>);
    /// more than 1 ⇒
    /// <see cref="Accounting.Application.Common.Exceptions.PaymentRequestVoucherAccountConfigException"/>.
    /// </summary>
    Task<IReadOnlyList<VoucherDetailTafsiliLinkInput>> ResolvePayablesLinksAsync(
        Guid payablesAccountId,
        Guid paymentRequestId,
        Guid? beneficiaryTafsiliId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// «حساب اعتبار مالیات بر ارزش‌افزوده» / «حساب بستانکاران بیمه» — must require ZERO تفصیلی
    /// levels; any configured level ⇒
    /// <see cref="Accounting.Application.Common.Exceptions.PaymentRequestVoucherAccountConfigException"/>
    /// naming <paramref name="accountLabel"/>.
    /// </summary>
    Task EnsureNoTafsiliRequiredAsync(
        Guid accountCodeId,
        string accountLabel,
        CancellationToken cancellationToken = default);
}
