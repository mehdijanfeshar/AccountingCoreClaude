using Accounting.Domain.Common;
using Accounting.Domain.Exceptions;
using Accounting.Domain.ValueObjects;

namespace Accounting.Domain.Entities;

/// <summary>
/// سطح «کل» در کدینگ ثابت. همیشه زیر یک <see cref="AccountGroup"/> تعریف می‌شود
/// و فقط از طریق <see cref="AccountGroup.AddGeneralLedgerAccount"/> ساخته می‌شود (قانون ۱).
///
/// <para>
/// <b>منسوخ (۲۰۲۶-۰۸-۱۷):</b> طبق تصمیم آگاهانهٔ کاربر، مدل نوشتن معتبر پروژه اکنون
/// Entityهای <c>Accounting.Domain.Legacy</c> (schema واقعی Oracle) هستند، نه این مدل Rich.
/// این کلاس صرفاً برای مستندسازی اجرایی (تست‌های موجود) نگه داشته شده و نباید در کد جدید
/// استفاده شود.
/// </para>
/// </summary>
[Obsolete("طبق تصمیم کاربر در ۲۰۲۶-۰۸-۱۷، مدل نوشتن معتبر پروژه Entityهای Accounting.Domain.Legacy است؛ این تایپ منسوخ است و برای کد جدید استفاده نشود.")]
public sealed class GeneralLedgerAccount
{
    /// <summary>طول ثابت کد سطح کل (قانون ۱).</summary>
    public const int CodeLength = 2;

    public int Id { get; private set; }
    public AccountCode Code { get; private set; } = null!;
    public string Title { get; private set; } = null!;

    public int AccountGroupId { get; private set; }
    public AccountGroup AccountGroup { get; private set; } = null!;

    private readonly List<SubsidiaryAccount> _subsidiaryAccounts = new();
    public IReadOnlyCollection<SubsidiaryAccount> SubsidiaryAccounts => _subsidiaryAccounts.AsReadOnly();

    // برای EF Core
    private GeneralLedgerAccount()
    {
    }

    private GeneralLedgerAccount(AccountGroup accountGroup, AccountCode code, string title)
    {
        AccountGroup = accountGroup;
        AccountGroupId = accountGroup.Id;
        Code = code;
        Title = Guard.Title(title);
    }

    /// <summary>
    /// internal تا فقط AccountGroup (والد صحیح) بتواند یک GeneralLedgerAccount بسازد؛
    /// از بیرونِ اسمبلی Domain (مثلاً Application) نمی‌توان مستقیماً new/Create زد.
    /// </summary>
    internal static GeneralLedgerAccount Create(AccountGroup accountGroup, AccountCode code, string title)
        => new(accountGroup, code, title);

    /// <summary>
    /// یک معین جدید زیر همین کل تعریف می‌کند. تنها راه ساخت SubsidiaryAccount همین متد است
    /// (قانون ۱: هر سطح فقط زیر والد درست خودش تعریف شود).
    /// </summary>
    public SubsidiaryAccount AddSubsidiaryAccount(string code, string title, AccountNature nature)
    {
        var accountCode = AccountCode.Create(code, SubsidiaryAccount.CodeLength);

        if (_subsidiaryAccounts.Any(s => s.Code.Equals(accountCode)))
        {
            throw new DuplicateAccountCodeException(
                $"کد معین «{accountCode.Value}» قبلاً در کل «{Code.Value}» تعریف شده است.");
        }

        var account = SubsidiaryAccount.Create(this, accountCode, title, nature);
        _subsidiaryAccounts.Add(account);
        return account;
    }
}
