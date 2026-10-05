using System.Globalization;
using Accounting.Application.Common.Exceptions;

namespace Accounting.Application.Common.BankDisk;

/// <summary>نوع سند بانکی یک ردیف دیسکت.</summary>
public enum RefahDiskDocType
{
    /// <summary>اعلامیهٔ بانک (نوع سند «0») — شماره ندارد.</summary>
    BankNotice = 0,

    /// <summary>چک (برداشت).</summary>
    Cheque = 1,

    /// <summary>فیش (واریز).</summary>
    Fish = 2,
}

/// <summary>یک ردیف دیسکت بانک رفاه، تفسیرشده.</summary>
public sealed record RefahDiskRecord(
    int LineNo,
    string Date,
    string AccountNumber,
    bool IsDeposit,
    long Amount,
    RefahDiskDocType DocType,
    string? Number);

/// <summary>
/// قالب دیسکت حساب جاری بانک رفاه — عین <c>ImportDisketCommandHandler</c> پروژهٔ مرجع
/// (<c>BankCartDetails\ImportDisk\RefahJari</c>). مشترک بین «کارت حساب جاری» (عملیات) و «مغایرت بانکی»
/// (خزانه‌داری):
/// <list type="bullet">
/// <item>نام فایل <c>STM001</c>؛ هر ردیف دقیقاً ۱۳۹ کاراکتر (ثابت‌عرض).</item>
/// <item>[1..8] تاریخ شمسی YYYYMMDD · [15..] شمارهٔ چک/فیش (۶ رقم برای تراکنش ۰، ۷ رقم برای ۱)
/// · [21] نوع تراکنش · [22..34] مبلغ (۱۳ رقم، ریال) · [59..76] شمارهٔ حساب (۱۸ رقم با صفر پیشرو)
/// · [137] نوع سند («0» اعلامیهٔ بانک).</item>
/// <item>جهت: اعلامیهٔ بانک تراکنش 1 = واریز؛ چک/فیش تراکنش 0 = واریز (فیش) و 1 = برداشت (چک).</item>
/// </list>
/// </summary>
public static class RefahDiskFormat
{
    public const int LineLength = 139;
    public const string FileName = "STM001";

    public static void EnsureFileName(string? fileName)
    {
        if (fileName is not null
            && !string.Equals(Path.GetFileNameWithoutExtension(fileName), FileName, StringComparison.OrdinalIgnoreCase))
        {
            throw new TreasuryBankStatementFileInvalidException($"نام فایل دیسکت بانک رفاه باید {FileName} باشد.");
        }
    }

    public static string NormalizeAccount(string? accountNumber)
        => (accountNumber ?? string.Empty).Trim().TrimStart('0');

    /// <summary>
    /// فایل را می‌خواند؛ هر ردیف نامعتبر یا ردیفی با شمارهٔ حساب دیگر ⇒ <see cref="TreasuryBankStatementFileInvalidException"/>.
    /// </summary>
    public static async Task<IReadOnlyList<RefahDiskRecord>> ParseAsync(
        Stream content, string? expectedAccountNumber, CancellationToken cancellationToken = default)
    {
        var expected = NormalizeAccount(expectedAccountNumber);
        using var reader = new StreamReader(content, System.Text.Encoding.ASCII);
        var result = new List<RefahDiskRecord>();
        var lineNo = 0;
        while (await reader.ReadLineAsync(cancellationToken) is { } raw)
        {
            lineNo++;
            var line = raw.TrimEnd('\r', '\n');
            if (line.Trim().Length == 0)
                continue;
            if (line.Length != LineLength)
                throw new TreasuryBankStatementFileInvalidException(
                    $"ردیف {lineNo}: طول هر ردیف باید {LineLength} کاراکتر باشد (این ردیف {line.Length}).");

            var account = NormalizeAccount(line.Substring(59, 18));
            if (expected.Length > 0 && account != expected)
                throw new TreasuryBankStatementFileInvalidException(
                    $"شمارهٔ حساب دیسکت ({account}) با حساب بانکی انتخابی ({expected}) مطابقت ندارد.");

            var date = line.Substring(1, 8);
            if (!date.All(char.IsAsciiDigit))
                throw new TreasuryBankStatementFileInvalidException($"ردیف {lineNo}: تاریخ نامعتبر است.");
            if (!long.TryParse(line.Substring(22, 13), NumberStyles.None, CultureInfo.InvariantCulture, out var amount) || amount <= 0)
                throw new TreasuryBankStatementFileInvalidException($"ردیف {lineNo}: مبلغ نامعتبر است.");

            var transType = line[21];
            if (line[137] == '0')
            {
                result.Add(new RefahDiskRecord(lineNo, date, account, transType == '1', amount, RefahDiskDocType.BankNotice, null));
                continue;
            }

            var isDeposit = transType == '0';
            var digits = line.Substring(15, isDeposit ? 6 : 7);
            var number = long.TryParse(digits, NumberStyles.None, CultureInfo.InvariantCulture, out var n)
                ? n.ToString(CultureInfo.InvariantCulture)
                : digits.Trim();
            result.Add(new RefahDiskRecord(
                lineNo, date, account, isDeposit, amount, isDeposit ? RefahDiskDocType.Fish : RefahDiskDocType.Cheque, number));
        }

        if (result.Count == 0)
            throw new TreasuryBankStatementFileInvalidException("فایل دیسکت ردیفی ندارد.");
        return result;
    }
}
