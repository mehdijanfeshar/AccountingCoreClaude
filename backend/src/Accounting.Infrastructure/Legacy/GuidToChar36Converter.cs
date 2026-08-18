using System;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Accounting.Infrastructure.Legacy;

/// <summary>
/// یک ValueConverter مشترک برای همهٔ ستون‌های Legacy از نوع CHAR(36) که در Oracle
/// شناسهٔ GUID را به‌صورت رشتهٔ ۳۶ کاراکتری با فرمت استاندارد dashed (کوچک‌حرف)
/// ذخیره می‌کنند. طبق بررسی کامل database-reverse-engineer روی دادهٔ زندهٔ
/// schema CENTRALACCOUNT (۱۲۴,۰۹۴ مقدار غیر-null در همهٔ ۱۷۷ ستون)، ۱۰۰٪ دادهٔ
/// موجود دقیقاً با فرمت "D" (8-4-4-4-12، dashed، lowercase) مطابقت دارد.
///
/// این converter عمداً به‌صورت سراسری/Convention (ConfigureConventions یا
/// Properties&lt;Guid&gt;()) اعمال نشده تا محدودهٔ تبدیل صراحتاً و قابل‌ممیزی
/// روی همان ۱۷۷ خاصیت شناخته‌شده باقی بماند؛ به‌جای آن روی تک‌تک Propertyها در
/// OnModelCreating فراخوانی می‌شود.
/// </summary>
public static class GuidToChar36Converter
{
    public static readonly ValueConverter<Guid, string> Instance = new(
        g => g.ToString("D").ToLowerInvariant(),
        s => Guid.ParseExact(s.Trim(), "D"));
}
