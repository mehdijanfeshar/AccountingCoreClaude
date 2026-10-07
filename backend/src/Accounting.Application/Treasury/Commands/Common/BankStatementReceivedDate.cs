using Accounting.Application.Common.Interfaces;

namespace Accounting.Application.Treasury.Commands.Common;

/// <summary>
/// تاریخ وصول (<c>DATE_RSID</c>) روی چک/فیش ردیف سندی که با ردیف صورت‌حساب بانک تطبیق خورد — عین کارت حساب
/// جاری (فاز ۴۴-ز) و مرجع؛ با برگرداندن تطبیق پاک می‌شود. هر دو تاریخ شمسی <c>yyyyMMdd</c>اند.
/// </summary>
internal static class BankStatementReceivedDate
{
    public static async Task SetAsync(
        IVoucherDetailRepository voucherDetails,
        IBankCardRepository? bankCards,
        Guid voucherDetailId,
        string? vahedCode,
        string? date,
        CancellationToken cancellationToken)
    {
        if (bankCards is null || string.IsNullOrEmpty(vahedCode))
            return;

        var detail = await voucherDetails.GetForUpdateAsync(voucherDetailId, vahedCode, cancellationToken);
        if (detail is null)
            return;

        if (detail.CHECK_ID is { } checkId)
            await bankCards.SetCheckReceivedDateAsync(checkId, date, cancellationToken);
        if (detail.RECEIP_ID is { } receiptId)
            await bankCards.SetReceiptReceivedDateAsync(receiptId, date, cancellationToken);
    }
}
