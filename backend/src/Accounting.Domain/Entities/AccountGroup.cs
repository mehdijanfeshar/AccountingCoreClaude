using Accounting.Domain.Common;
using Accounting.Domain.Exceptions;
using Accounting.Domain.ValueObjects;

namespace Accounting.Domain.Entities;

/// <summary>
/// ریشهٔ تجمیع (Aggregate Root) ساختار کدینگ ثابتِ حسابداری: گروه → کل → معین.
/// این سه سطح همیشه با هم بارگذاری/ذخیره می‌شوند (از طریق Repository مخصوص AccountGroup)؛
/// GeneralLedgerAccount و SubsidiaryAccount موجودیت‌های فرزند همین Aggregate هستند،
/// نه Aggregate Root مستقل. سطح تفصیلی (DetailAccountType/DetailAccount) خارج از این
/// Aggregate و به‌صورت داده‌های مرجع مستقل تعریف می‌شود؛ اتصال آن‌ها به معین صرفاً از طریق
/// شناسه (Id) در SubsidiaryDetailTypeLink برقرار می‌شود.
///
/// <para>
/// <b>منسوخ (۲۰۲۶-۰۸-۱۷):</b> طبق تصمیم آگاهانهٔ کاربر، مدل نوشتن معتبر پروژه اکنون
/// Entityهای <c>Accounting.Domain.Legacy</c> (schema واقعی Oracle) هستند، نه این مدل Rich.
/// این کلاس صرفاً برای مستندسازی اجرایی (تست‌های موجود) نگه داشته شده و نباید در کد جدید
/// استفاده شود.
/// </para>
/// </summary>
[Obsolete("طبق تصمیم کاربر در ۲۰۲۶-۰۸-۱۷، مدل نوشتن معتبر پروژه Entityهای Accounting.Domain.Legacy است؛ این تایپ منسوخ است و برای کد جدید استفاده نشود.")]
public sealed class AccountGroup
{
    /// <summary>طول ثابت کد سطح گروه (قانون ۱).</summary>
    public const int CodeLength = 1;

    public int Id { get; private set; }
    public AccountCode Code { get; private set; } = null!;
    public string Title { get; private set; } = null!;

    private readonly List<GeneralLedgerAccount> _generalLedgerAccounts = new();
    public IReadOnlyCollection<GeneralLedgerAccount> GeneralLedgerAccounts => _generalLedgerAccounts.AsReadOnly();

    // برای EF Core (Materialization بدون فراخوانی سازندهٔ عمومی)
    private AccountGroup()
    {
    }

    private AccountGroup(AccountCode code, string title)
    {
        Code = code;
        Title = Guard.Title(title);
    }

    public static AccountGroup Create(string code, string title)
        => new(AccountCode.Create(code, CodeLength), title);

    /// <summary>
    /// یک کل جدید زیر همین گروه تعریف می‌کند. تنها راه ساخت GeneralLedgerAccount همین متد است؛
    /// در نتیجه ساختن یک کل بدون گروه والد معتبر ساختاراً غیرممکن است (قانون ۱).
    /// </summary>
    public GeneralLedgerAccount AddGeneralLedgerAccount(string code, string title)
    {
        var accountCode = AccountCode.Create(code, GeneralLedgerAccount.CodeLength);

        if (_generalLedgerAccounts.Any(g => g.Code.Equals(accountCode)))
        {
            throw new DuplicateAccountCodeException(
                $"کد کل «{accountCode.Value}» قبلاً در گروه «{Code.Value}» تعریف شده است.");
        }

        var account = GeneralLedgerAccount.Create(this, accountCode, title);
        _generalLedgerAccounts.Add(account);
        return account;
    }
}
