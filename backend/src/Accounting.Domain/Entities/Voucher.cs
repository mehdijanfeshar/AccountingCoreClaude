using Accounting.Domain.Exceptions;
using Accounting.Domain.Rules;
using Accounting.Domain.ValueObjects;

namespace Accounting.Domain.Entities;

/// <summary>
/// سند حسابداری؛ ریشهٔ تجمیع (Aggregate Root) مستقل از AccountGroup. ردیف‌های سند
/// (<see cref="VoucherLine"/>) فقط شناسهٔ معین (SubsidiaryAccountId) و اسنپ‌شات سیاست
/// تفصیلی آن را نگه می‌دارند، نه ارجاع زنده به SubsidiaryAccount (رجوع به SubsidiaryDetailPolicy).
/// سند تا وقتی Draft است قابل ویرایش است؛ پس از Post غیرقابل تغییر (immutable) می‌شود (قانون ۶).
///
/// <para>
/// <b>منسوخ (۲۰۲۶-۰۸-۱۷):</b> طبق تصمیم آگاهانهٔ کاربر، مدل نوشتن معتبر پروژه اکنون
/// Entityهای <c>Accounting.Domain.Legacy</c> (schema واقعی Oracle) هستند، نه این مدل Rich.
/// این کلاس صرفاً برای مستندسازی اجرایی (تست‌های موجود) نگه داشته شده و نباید در کد جدید
/// استفاده شود.
/// </para>
/// </summary>
[Obsolete("طبق تصمیم کاربر در ۲۰۲۶-۰۸-۱۷، مدل نوشتن معتبر پروژه Entityهای Accounting.Domain.Legacy است؛ این تایپ منسوخ است و برای کد جدید استفاده نشود.")]
public sealed class Voucher
{
    public int Id { get; private set; }
    public string Number { get; private set; } = null!;
    public DateTime Date { get; private set; }
    public VoucherStatus Status { get; private set; }
    public DateTime? PostedAtUtc { get; private set; }

    private readonly List<VoucherLine> _lines = new();
    public IReadOnlyCollection<VoucherLine> Lines => _lines.AsReadOnly();

    public Money TotalDebit => _lines.Aggregate(Money.Zero, (sum, line) => sum + line.Debit);
    public Money TotalCredit => _lines.Aggregate(Money.Zero, (sum, line) => sum + line.Credit);
    public bool IsBalanced => TotalDebit == TotalCredit;

    // برای EF Core
    private Voucher()
    {
    }

    private Voucher(string number, DateTime date)
    {
        Number = ValidateNumber(number);
        Date = date;
        Status = VoucherStatus.Draft;
    }

    public static Voucher CreateDraft(string number, DateTime date) => new(number, date);

    /// <summary>
    /// یک ردیف جدید به سند اضافه می‌کند. اعتبارسنجی تفصیلی الزامی/غیرمجاز/تکراری همین‌جا،
    /// در لحظهٔ ساخت VoucherLine، انجام می‌شود (قانون ۵؛ مسیر الف در chart-of-accounts.md).
    /// </summary>
    public VoucherLine AddLine(
        SubsidiaryDetailPolicy subsidiaryPolicy,
        Money debit,
        Money credit,
        IEnumerable<DetailAccount>? detailAccounts = null)
    {
        EnsureDraft();

        var line = VoucherLine.Create(this, subsidiaryPolicy, debit, credit, detailAccounts);
        _lines.Add(line);
        return line;
    }

    public void RemoveLine(VoucherLine line)
    {
        EnsureDraft();
        _lines.Remove(line);
    }

    /// <summary>
    /// سند را Post می‌کند. قبل از تغییر وضعیت، کل سند توسط <see cref="VoucherPostingValidator"/>
    /// (مسیر ب) بازبینی می‌شود: بدون ردیف نبودن، تراز بودن، و صحت تفصیلی هر ردیف (قانون ۵ و ۶).
    /// در صورت شکست هر بخش از اعتبارسنجی، یک DomainException پرتاب و وضعیت سند بدون تغییر می‌ماند.
    /// </summary>
    public void Post()
    {
        EnsureDraft();

        new VoucherPostingValidator().Validate(this);

        Status = VoucherStatus.Posted;
        PostedAtUtc = DateTime.UtcNow;
    }

    private void EnsureDraft()
    {
        if (Status == VoucherStatus.Posted)
            throw new VoucherImmutableException(Number);
    }

    private static string ValidateNumber(string? number)
    {
        if (string.IsNullOrWhiteSpace(number))
            throw new InvalidVoucherNumberException("شمارهٔ سند نمی‌تواند خالی باشد.");

        return number.Trim();
    }
}
