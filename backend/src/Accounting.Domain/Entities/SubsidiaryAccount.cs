using Accounting.Domain.Common;
using Accounting.Domain.Exceptions;
using Accounting.Domain.ValueObjects;

namespace Accounting.Domain.Entities;

/// <summary>
/// سطح «معین» در کدینگ ثابت؛ دارای ماهیت بدهکار/بستانکار (قانون ۲) و صاحبِ سیاست شناور
/// تفصیلی (قانون ۳ و ۴) از طریق <see cref="DetailTypeLinks"/>. توجه مهم (قانون ۴): این‌جا
/// هیچ‌گاه یک DetailAccount مشخص ثبت نمی‌شود؛ فقط «نوع» تفصیلی (DetailAccountType) و حالت
/// الزام آن لینک می‌شود. مقدار واقعی تفصیلی فقط در VoucherLineDetailValue (زمان صدور سند)
/// ظاهر می‌شود — این تضمین ساختاری است، نه صرفاً قراردادی.
///
/// <para>
/// <b>منسوخ (۲۰۲۶-۰۸-۱۷):</b> طبق تصمیم آگاهانهٔ کاربر، مدل نوشتن معتبر پروژه اکنون
/// Entityهای <c>Accounting.Domain.Legacy</c> (schema واقعی Oracle) هستند، نه این مدل Rich.
/// این کلاس صرفاً برای مستندسازی اجرایی (تست‌های موجود) نگه داشته شده و نباید در کد جدید
/// استفاده شود.
/// </para>
/// </summary>
[Obsolete("طبق تصمیم کاربر در ۲۰۲۶-۰۸-۱۷، مدل نوشتن معتبر پروژه Entityهای Accounting.Domain.Legacy است؛ این تایپ منسوخ است و برای کد جدید استفاده نشود.")]
public sealed class SubsidiaryAccount
{
    /// <summary>طول ثابت کد سطح معین (قانون ۱).</summary>
    public const int CodeLength = 3;

    public int Id { get; private set; }
    public AccountCode Code { get; private set; } = null!;
    public string Title { get; private set; } = null!;
    public AccountNature Nature { get; private set; }

    public int GeneralLedgerAccountId { get; private set; }
    public GeneralLedgerAccount GeneralLedgerAccount { get; private set; } = null!;

    private readonly List<SubsidiaryDetailTypeLink> _detailTypeLinks = new();
    public IReadOnlyCollection<SubsidiaryDetailTypeLink> DetailTypeLinks => _detailTypeLinks.AsReadOnly();

    // برای EF Core
    private SubsidiaryAccount()
    {
    }

    private SubsidiaryAccount(GeneralLedgerAccount generalLedgerAccount, AccountCode code, string title, AccountNature nature)
    {
        GeneralLedgerAccount = generalLedgerAccount;
        GeneralLedgerAccountId = generalLedgerAccount.Id;
        Code = code;
        Title = Guard.Title(title);
        Nature = nature;
    }

    /// <summary>internal تا فقط GeneralLedgerAccount (والد صحیح) بتواند SubsidiaryAccount بسازد.</summary>
    internal static SubsidiaryAccount Create(GeneralLedgerAccount generalLedgerAccount, AccountCode code, string title, AccountNature nature)
        => new(generalLedgerAccount, code, title, nature);

    /// <summary>
    /// کد کامل معین = کد گروه + کد کل + کد معین (قانون ۱). فقط زمانی معتبر است که کل زنجیرهٔ
    /// Aggregate (AccountGroup → GeneralLedgerAccount → SubsidiaryAccount) بارگذاری شده باشد؛
    /// چون AccountGroup ریشهٔ Aggregate است، Repository مربوطه باید همیشه این زنجیره را کامل بار کند.
    /// </summary>
    public string FullCode => GeneralLedgerAccount.AccountGroup.Code.Value + GeneralLedgerAccount.Code.Value + Code.Value;

    /// <summary>
    /// یک نوع تفصیلی را به این معین متصل می‌کند. تکرار همان نوع مجاز نیست (قانون ۳).
    /// </summary>
    public SubsidiaryDetailTypeLink LinkDetailType(DetailAccountType detailType, DetailRequirement requirement)
    {
        if (_detailTypeLinks.Any(l => l.DetailAccountTypeId == detailType.Id))
        {
            throw new DuplicateDetailTypeLinkException(
                $"نوع تفصیلی «{detailType.Title}» قبلاً به معین «{Code.Value}» متصل شده است.");
        }

        var link = SubsidiaryDetailTypeLink.Create(this, detailType, requirement, _detailTypeLinks.Count);
        _detailTypeLinks.Add(link);
        return link;
    }

    /// <summary>
    /// قطع اتصال یک نوع تفصیلی از این معین. پس از این عملیات، آن نوع برای این معین
    /// «غیرمجاز» محسوب می‌شود (لینک‌نشده = غیرمجاز؛ قانون ۳).
    /// </summary>
    public void UnlinkDetailType(int detailAccountTypeId)
    {
        var link = _detailTypeLinks.FirstOrDefault(l => l.DetailAccountTypeId == detailAccountTypeId);
        if (link is not null)
            _detailTypeLinks.Remove(link);
    }

    public void ChangeDetailTypeRequirement(int detailAccountTypeId, DetailRequirement requirement)
    {
        var link = _detailTypeLinks.FirstOrDefault(l => l.DetailAccountTypeId == detailAccountTypeId)
            ?? throw new DetailValueNotAllowedException(Id, detailAccountTypeId);

        link.ChangeRequirement(requirement);
    }

    /// <summary>
    /// اسنپ‌شات فقط‌خواندنیِ سیاست تفصیلی این معین، برای عبور به Aggregate جدای Voucher
    /// بدون نگه‌داشتن ارجاع مستقیم به این Entity (رجوع به مستندات SubsidiaryDetailPolicy).
    /// </summary>
    public SubsidiaryDetailPolicy GetDetailPolicy()
        => SubsidiaryDetailPolicy.Create(Id, _detailTypeLinks.Select(l => (l.DetailAccountTypeId, l.Requirement)));
}
