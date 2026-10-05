using Accounting.Application.Reports.AttributeAccountReconciliation;

namespace Accounting.Application.Common.Interfaces;

/// <summary>
/// سمت خواندن «مغایرت‌گیری حساب‌های شناسه‌دار».
///
/// <para>
/// ⚠️ مستقیم از جدول‌ها، نه از Viewهای مرجع — استثنای ثبت‌شدهٔ قاعدهٔ ۹ (ریسک #۳۰): آن Viewها
/// فیلتر واحد/سال/حذف ندارند و یکی‌شان ضرب دکارتی می‌سازد.
/// </para>
/// </summary>
public interface IAttributeAccountReconciliationReadRepository
{
    /// <summary>
    /// ردیف‌های سند (حذف‌نشده) روی معین‌هایی که در همان واحد و سال تعریف شناسه دارند، هرکدام با
    /// مقدار شناسهٔ ثبت‌شده‌اش. <see cref="AttributeAccountFilter.AccountId"/> اختیاری است.
    /// </summary>
    Task<IReadOnlyList<AttributeAccountRawLine>> GetLinesAsync(
        AttributeAccountFilter filter,
        CancellationToken cancellationToken = default);
}
