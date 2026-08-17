using Accounting.Domain.ValueObjects;

namespace Accounting.Domain.Entities;

/// <summary>
/// خودِ «شناوری»: پیوند بین یک معین و یک نوع تفصیلی، همراه با حالت الزام آن.
/// فقط از طریق <see cref="SubsidiaryAccount.LinkDetailType"/> ساخته می‌شود تا تکرار
/// یک نوع تفصیلی برای یک معین (قانون ۳) ساختاراً کنترل‌شده بماند.
///
/// <para>
/// <b>منسوخ (۲۰۲۶-۰۸-۱۷):</b> طبق تصمیم آگاهانهٔ کاربر، مدل نوشتن معتبر پروژه اکنون
/// Entityهای <c>Accounting.Domain.Legacy</c> (schema واقعی Oracle) هستند، نه این مدل Rich.
/// این کلاس صرفاً برای مستندسازی اجرایی (تست‌های موجود) نگه داشته شده و نباید در کد جدید
/// استفاده شود.
/// </para>
/// </summary>
[Obsolete("طبق تصمیم کاربر در ۲۰۲۶-۰۸-۱۷، مدل نوشتن معتبر پروژه Entityهای Accounting.Domain.Legacy است؛ این تایپ منسوخ است و برای کد جدید استفاده نشود.")]
public sealed class SubsidiaryDetailTypeLink
{
    public int Id { get; private set; }

    public int SubsidiaryAccountId { get; private set; }
    public SubsidiaryAccount SubsidiaryAccount { get; private set; } = null!;

    public int DetailAccountTypeId { get; private set; }
    public DetailAccountType DetailAccountType { get; private set; } = null!;

    public DetailRequirement Requirement { get; private set; }

    /// <summary>ترتیب نمایش فیلدهای تفصیلی در فرم صدور سند (مدیریت‌شده توسط frontend-react).</summary>
    public int DisplayOrder { get; private set; }

    public bool IsRequired => Requirement == DetailRequirement.Required;

    // برای EF Core
    private SubsidiaryDetailTypeLink()
    {
    }

    private SubsidiaryDetailTypeLink(
        SubsidiaryAccount subsidiaryAccount,
        DetailAccountType detailAccountType,
        DetailRequirement requirement,
        int displayOrder)
    {
        SubsidiaryAccount = subsidiaryAccount;
        SubsidiaryAccountId = subsidiaryAccount.Id;
        DetailAccountType = detailAccountType;
        DetailAccountTypeId = detailAccountType.Id;
        Requirement = requirement;
        DisplayOrder = displayOrder;
    }

    internal static SubsidiaryDetailTypeLink Create(
        SubsidiaryAccount subsidiaryAccount,
        DetailAccountType detailAccountType,
        DetailRequirement requirement,
        int displayOrder)
        => new(subsidiaryAccount, detailAccountType, requirement, displayOrder);

    internal void ChangeRequirement(DetailRequirement requirement) => Requirement = requirement;
}
