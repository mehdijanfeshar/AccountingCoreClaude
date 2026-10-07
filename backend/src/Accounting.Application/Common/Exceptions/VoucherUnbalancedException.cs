namespace Accounting.Application.Common.Exceptions;

/// <summary>
/// سند ناتراز (یا بدون ردیف) نمی‌تواند از «یادداشت» خارج شود — تصمیم صاحب پروژه ۲۰۲۶-۱۰-۰۷
/// (ریسک #۳). یادداشت می‌تواند ناتراز باشد تا حسابدار کامل کند؛ رفتن به موقت و بعد از آن
/// تراز می‌خواهد. 409 مثل <see cref="VoucherNotEditableException"/>: دربارهٔ وضعیت داده است و
/// کاربر خودش می‌تواند درستش کند.
/// </summary>
public sealed class VoucherUnbalancedException : Exception
{
    public VoucherUnbalancedException(Guid voucherHeadId, string? docNum, decimal debtor, decimal creditor, int lineCount)
        : base($"Voucher {voucherHeadId} is unbalanced (debtor {debtor}, creditor {creditor}, lines {lineCount}).")
    {
        VoucherHeadId = voucherHeadId;
        PublicDetail = lineCount == 0
            ? $"سند {Label(docNum)} ردیف ندارد و نمی‌تواند از «یادداشت» خارج شود."
            : $"سند {Label(docNum)} تراز نیست (جمع بدهکار {debtor:N0}، جمع بستانکار {creditor:N0}، اختلاف {Math.Abs(debtor - creditor):N0}). " +
              "سند ناتراز فقط در وضعیت «یادداشت» می‌ماند.";
    }

    public Guid VoucherHeadId { get; }

    public string PublicDetail { get; }

    private static string Label(string? docNum) => string.IsNullOrWhiteSpace(docNum) ? "" : $"شمارهٔ {docNum}";
}
