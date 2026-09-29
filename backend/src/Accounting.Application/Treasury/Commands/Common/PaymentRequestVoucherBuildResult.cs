using Accounting.Application.Vouchers.Commands.Common;

namespace Accounting.Application.Treasury.Commands.Common;

/// <summary>
/// One staged <c>TB_VOUCHERSDETAIL</c> line of a بخش-۴-ب automatic voucher, echoed back by
/// <see cref="IPaymentRequestLiabilityVoucherBuilder"/>/<see cref="IPaymentRequestPaymentVoucherBuilder"/>
/// so a caller that also has to mirror the line elsewhere (<c>ExecutePaymentRequestCommandHandler</c>
/// mirroring voucher 2 into <c>TB_PAYRECIVDETAIL</c>) never re-derives the همان تفصیلی resolution
/// logic a second time — it reads it back from here instead.
/// </summary>
public sealed record PaymentRequestVoucherLineResult(
    Guid? AccountCodeId,
    decimal Debit,
    decimal Credit,
    IReadOnlyList<VoucherDetailTafsiliLinkInput> TafsiliLinks);

/// <summary>Result of staging one بخش-۴-ب automatic <c>TB_VOUCHERSHEAD</c>/<c>TB_VOUCHERSDETAIL</c> voucher.</summary>
public sealed record PaymentRequestVoucherBuildResult(
    Guid VoucherHeadId,
    string DocNum,
    string DateDoc,
    string Year,
    IReadOnlyList<PaymentRequestVoucherLineResult> Lines);
