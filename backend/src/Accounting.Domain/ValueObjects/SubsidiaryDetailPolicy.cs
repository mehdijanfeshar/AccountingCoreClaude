using Accounting.Domain.Exceptions;

namespace Accounting.Domain.ValueObjects;

/// <summary>
/// اسنپ‌شات فقط‌خواندنیِ سیاست تفصیلیِ یک معین در یک لحظهٔ مشخص: کدام انواع تفصیلی
/// به آن معین مجازند و کدام‌یک الزامی‌اند (خروجی SubsidiaryAccount.GetDetailPolicy()).
///
/// چرا این VO لازم است:
/// Voucher/VoucherLine یک Aggregate کاملاً جدا از AccountGroup (که SubsidiaryAccount در آن
/// زندگی می‌کند) هستند و طبق اصول DDD نباید ارجاع زنده (object reference) به Aggregate دیگر
/// نگه دارند. از طرفی Domain هم نباید به دیتابیس دسترسی داشته باشد تا در لحظهٔ Post دوباره
/// سیاست تفصیلی را از جایی بخواند. راه‌حل: لایهٔ Application، SubsidiaryAccount را (از طریق
/// Repository خودش) بارگذاری می‌کند، GetDetailPolicy() را صدا می‌زند تا یک اسنپ‌شات فقط‌خواندنی
/// و کاملاً درون‌حافظه‌ای بگیرد، و همین اسنپ‌شات (نه خودِ SubsidiaryAccount) را به
/// Voucher.AddLine(...) می‌دهد. VoucherLine این اسنپ‌شات را به‌عنوان بخشی از خودش نگه می‌دارد
/// تا هم در لحظهٔ ساخت (Option A) و هم در لحظهٔ Post توسط VoucherPostingValidator (Option B)
/// بتواند بدون هیچ وابستگی بیرونی دوباره اعتبارسنجی شود.
///
/// <para>
/// <b>منسوخ (۲۰۲۶-۰۸-۱۷):</b> طبق تصمیم آگاهانهٔ کاربر، مدل نوشتن معتبر پروژه اکنون
/// Entityهای <c>Accounting.Domain.Legacy</c> (schema واقعی Oracle) هستند، نه این مدل Rich.
/// این کلاس صرفاً برای مستندسازی اجرایی (تست‌های موجود) نگه داشته شده و نباید در کد جدید
/// استفاده شود.
/// </para>
/// </summary>
[Obsolete("طبق تصمیم کاربر در ۲۰۲۶-۰۸-۱۷، مدل نوشتن معتبر پروژه Entityهای Accounting.Domain.Legacy است؛ این تایپ منسوخ است و برای کد جدید استفاده نشود.")]
public sealed class SubsidiaryDetailPolicy
{
    public int SubsidiaryAccountId { get; }

    private readonly IReadOnlyDictionary<int, DetailRequirement> _rules;

    private SubsidiaryDetailPolicy(int subsidiaryAccountId, IReadOnlyDictionary<int, DetailRequirement> rules)
    {
        SubsidiaryAccountId = subsidiaryAccountId;
        _rules = rules;
    }

    public static SubsidiaryDetailPolicy Create(
        int subsidiaryAccountId,
        IEnumerable<(int DetailAccountTypeId, DetailRequirement Requirement)> rules)
    {
        var dict = new Dictionary<int, DetailRequirement>();

        foreach (var (detailAccountTypeId, requirement) in rules)
        {
            if (!dict.TryAdd(detailAccountTypeId, requirement))
            {
                throw new DuplicateDetailTypeLinkException(
                    $"نوع تفصیلی با شناسهٔ {detailAccountTypeId} بیش از یک بار در سیاست تفصیلی معین {subsidiaryAccountId} وجود دارد.");
            }
        }

        return new SubsidiaryDetailPolicy(subsidiaryAccountId, dict);
    }

    public static SubsidiaryDetailPolicy Empty(int subsidiaryAccountId)
        => new(subsidiaryAccountId, new Dictionary<int, DetailRequirement>());

    /// <summary>آیا این نوع تفصیلی اصلاً به معین وصل است (Optional یا Required)؟ لینک‌نشده = false.</summary>
    public bool IsAllowed(int detailAccountTypeId) => _rules.ContainsKey(detailAccountTypeId);

    public bool IsRequired(int detailAccountTypeId)
        => _rules.TryGetValue(detailAccountTypeId, out var requirement) && requirement == DetailRequirement.Required;

    public IReadOnlyCollection<int> GetRequiredTypeIds()
        => _rules.Where(kv => kv.Value == DetailRequirement.Required).Select(kv => kv.Key).ToArray();

    public IReadOnlyCollection<int> GetAllowedTypeIds() => _rules.Keys.ToArray();
}
