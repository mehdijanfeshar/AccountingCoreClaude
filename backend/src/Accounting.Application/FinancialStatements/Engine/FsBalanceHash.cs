using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace Accounting.Application.FinancialStatements.Engine;

/// <summary>
/// اثر انگشت ماندهٔ منبع یک اجرا (بخش ۴۵-ه): SHA-256 همهٔ ردیف‌های (ستون، معین، واحد، چهار مبلغ) به ترتیب ثابت.
/// اگر محاسبهٔ امروز با همان پارامترها اثر دیگری بدهد، اسناد پس از اجرا عوض شده‌اند ⇒ اجرا «کهنه» است.
/// </summary>
public static class FsBalanceHash
{
    public static string Compute(IReadOnlyDictionary<string, IReadOnlyList<FsAccountBalance>> balancesByColumn)
    {
        var sb = new StringBuilder();

        foreach (var (column, balances) in balancesByColumn.OrderBy(kv => kv.Key, StringComparer.Ordinal))
        {
            foreach (var b in balances
                         .OrderBy(b => b.AccCode, StringComparer.Ordinal)
                         .ThenBy(b => b.VahedCode ?? string.Empty, StringComparer.Ordinal))
            {
                sb.Append(column).Append('|').Append(b.AccCode).Append('|').Append(b.VahedCode).Append('|')
                    .Append(b.OpeningDebtor.ToString(CultureInfo.InvariantCulture)).Append('|')
                    .Append(b.OpeningCreditor.ToString(CultureInfo.InvariantCulture)).Append('|')
                    .Append(b.PeriodDebtor.ToString(CultureInfo.InvariantCulture)).Append('|')
                    .Append(b.PeriodCreditor.ToString(CultureInfo.InvariantCulture)).Append('\n');
            }
        }

        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(sb.ToString()))).ToLowerInvariant();
    }
}
