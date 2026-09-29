using Accounting.Application.Treasury.Queries;

namespace Accounting.Application.Common.Interfaces;

/// <summary>
/// Projects one <c>TB_VOUCHERSHEAD</c>/<c>TB_VOUCHERSDETAIL</c> tree (account code/name, تفصیلی
/// labels per line) into <see cref="PaymentRequestVoucherAccountingDto"/> — extracted out of
/// <c>PaymentRequestReadRepository.GetVoucherAccountingAsync</c> (بخش ۴-ب) so خزانه‌داری بخش ۴-ج's
/// <c>GET receipts/{id}/accounting</c>/<c>GET transfers/{id}/accounting</c> can return "the same
/// voucher shape as <c>payment-requests/{id}/accounting</c>" (owner instruction) without copying
/// the projection logic a third time. Deliberately reuses the بخش-۴-ب DTO record rather than a
/// module-specific clone — the shape genuinely has nothing payment-request-specific in it (account
/// code/name, تفصیلی labels, debit/credit), so a new near-identical record would just be
/// duplication with extra steps.
/// </summary>
public interface IVoucherAccountingReader
{
    /// <summary>
    /// Returns <see langword="null"/> when <paramref name="voucherHeadId"/> is <see langword="null"/>
    /// or no such <c>TB_VOUCHERSHEAD</c> row exists.
    /// </summary>
    Task<PaymentRequestVoucherAccountingDto?> GetAsync(Guid? voucherHeadId, CancellationToken cancellationToken = default);
}
