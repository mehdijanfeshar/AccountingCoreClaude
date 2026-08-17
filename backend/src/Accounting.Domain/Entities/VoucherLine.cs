using Accounting.Domain.Exceptions;
using Accounting.Domain.ValueObjects;

namespace Accounting.Domain.Entities;

/// <summary>
/// یک ردیف سند. طبق تصمیم طراحی «مسیر الف» (رجوع به chart-of-accounts.md)، تنها راه ساخت
/// VoucherLine گرفتنِ یک <see cref="SubsidiaryDetailPolicy"/> معتبر است؛ در نتیجه ساختن یک
/// ردیف با مقدار تفصیلی نامعتبر (الزامی جاافتاده، غیرمجاز، یا تکراری) از اساس غیرممکن است
/// (قانون ۵). این اعتبارسنجی مستقل از این‌که DetailAccount چگونه انتخاب شده، دوباره در
/// زمان Post توسط <see cref="Rules.VoucherPostingValidator"/> نیز تکرار می‌شود (مسیر ب) —
/// چون EF Core هنگام بازیابی از دیتابیس این سازنده را دور می‌زند و مستقیم فیلدها را پر می‌کند.
///
/// <para>
/// <b>منسوخ (۲۰۲۶-۰۸-۱۷):</b> طبق تصمیم آگاهانهٔ کاربر، مدل نوشتن معتبر پروژه اکنون
/// Entityهای <c>Accounting.Domain.Legacy</c> (schema واقعی Oracle) هستند، نه این مدل Rich.
/// این کلاس صرفاً برای مستندسازی اجرایی (تست‌های موجود) نگه داشته شده و نباید در کد جدید
/// استفاده شود.
/// </para>
/// </summary>
[Obsolete("طبق تصمیم کاربر در ۲۰۲۶-۰۸-۱۷، مدل نوشتن معتبر پروژه Entityهای Accounting.Domain.Legacy است؛ این تایپ منسوخ است و برای کد جدید استفاده نشود.")]
public sealed class VoucherLine
{
    public int Id { get; private set; }

    public int VoucherId { get; private set; }
    public Voucher Voucher { get; private set; } = null!;

    public int SubsidiaryAccountId { get; private set; }

    public Money Debit { get; private set; } = null!;
    public Money Credit { get; private set; } = null!;

    private readonly List<VoucherLineDetailValue> _detailValues = new();
    public IReadOnlyCollection<VoucherLineDetailValue> DetailValues => _detailValues.AsReadOnly();

    /// <summary>
    /// اسنپ‌شاتِ سیاست تفصیلیِ معین این ردیف، در لحظهٔ ساخت ردیف. برای بازبینی مستقل از
    /// دیتابیس در زمان Post (VoucherPostingValidator) نگه‌داری می‌شود.
    /// </summary>
    public SubsidiaryDetailPolicy DetailPolicySnapshot { get; private set; } = null!;

    // برای EF Core
    private VoucherLine()
    {
    }

    private VoucherLine(
        Voucher voucher,
        SubsidiaryDetailPolicy subsidiaryPolicy,
        Money debit,
        Money credit,
        IEnumerable<DetailAccount> detailAccounts)
    {
        Voucher = voucher;
        VoucherId = voucher.Id;
        SubsidiaryAccountId = subsidiaryPolicy.SubsidiaryAccountId;
        DetailPolicySnapshot = subsidiaryPolicy;

        ValidateAmounts(debit, credit);
        Debit = debit;
        Credit = credit;

        AttachDetailValues(detailAccounts);
    }

    /// <summary>internal تا فقط Voucher (والد صحیح) بتواند یک VoucherLine بسازد.</summary>
    internal static VoucherLine Create(
        Voucher voucher,
        SubsidiaryDetailPolicy subsidiaryPolicy,
        Money debit,
        Money credit,
        IEnumerable<DetailAccount>? detailAccounts)
        => new(voucher, subsidiaryPolicy, debit, credit, detailAccounts ?? Array.Empty<DetailAccount>());

    private void AttachDetailValues(IEnumerable<DetailAccount> detailAccounts)
    {
        var seenTypeIds = new HashSet<int>();

        foreach (var detailAccount in detailAccounts)
        {
            // نوع تفصیلی مستقیماً از خودِ DetailAccount خوانده می‌شود (نه از یک شناسهٔ خام جداگانه)؛
            // در نتیجه «عدم تطابق نوع» (قانون ۵) از اساس غیرممکن است، نه صرفاً رد می‌شود.
            var detailAccountTypeId = detailAccount.DetailAccountTypeId;

            if (!DetailPolicySnapshot.IsAllowed(detailAccountTypeId))
                throw new DetailValueNotAllowedException(SubsidiaryAccountId, detailAccountTypeId);

            if (!seenTypeIds.Add(detailAccountTypeId))
                throw new DuplicateDetailValueException(SubsidiaryAccountId, detailAccountTypeId);

            _detailValues.Add(VoucherLineDetailValue.Create(this, detailAccountTypeId, detailAccount.Id));
        }

        var missing = DetailPolicySnapshot.GetRequiredTypeIds().Except(seenTypeIds).ToArray();
        if (missing.Length > 0)
            throw new RequiredDetailValueMissingException(SubsidiaryAccountId, missing);
    }

    private static void ValidateAmounts(Money debit, Money credit)
    {
        // منفی بودن از قبل توسط خودِ Money.Of غیرممکن شده است.
        var hasDebit = debit.Amount > 0;
        var hasCredit = credit.Amount > 0;

        if (hasDebit && hasCredit)
            throw new InvalidVoucherLineAmountException("هر ردیف سند فقط می‌تواند یا بدهکار یا بستانکار داشته باشد، نه هر دو.");

        if (!hasDebit && !hasCredit)
            throw new InvalidVoucherLineAmountException("ردیف سند نمی‌تواند هم بدهکار و هم بستانکار صفر داشته باشد.");
    }

    /// <summary>
    /// بازبینیِ مستقل (Option ب) با استفاده از داده‌های همین ردیف (بدون نیاز به دیتابیس)؛
    /// توسط VoucherPostingValidator در لحظهٔ Post فراخوانی می‌شود.
    /// </summary>
    internal void ReValidateForPosting()
    {
        var providedTypeIds = _detailValues.Select(v => v.DetailAccountTypeId).ToArray();

        var duplicateGroup = providedTypeIds.GroupBy(id => id).FirstOrDefault(g => g.Count() > 1);
        if (duplicateGroup is not null)
            throw new DuplicateDetailValueException(SubsidiaryAccountId, duplicateGroup.Key);

        foreach (var typeId in providedTypeIds)
        {
            if (!DetailPolicySnapshot.IsAllowed(typeId))
                throw new DetailValueNotAllowedException(SubsidiaryAccountId, typeId);
        }

        var missingRequired = DetailPolicySnapshot.GetRequiredTypeIds().Except(providedTypeIds).ToArray();
        if (missingRequired.Length > 0)
            throw new RequiredDetailValueMissingException(SubsidiaryAccountId, missingRequired);

        ValidateAmounts(Debit, Credit);
    }
}
