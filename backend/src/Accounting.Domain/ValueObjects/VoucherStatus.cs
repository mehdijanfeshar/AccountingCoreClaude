namespace Accounting.Domain.ValueObjects;

/// <summary>
/// وضعیت سند. طبق قانون ۶، انتقال از Draft به Posted فقط از طریق Voucher.Post()
/// و پس از عبور موفق از VoucherPostingValidator ممکن است؛ Posted تغییرناپذیر (immutable) است.
/// </summary>
public enum VoucherStatus
{
    Draft = 1,
    Posted = 2
}
