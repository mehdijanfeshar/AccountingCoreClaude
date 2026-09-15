namespace Accounting.Domain.ValueObjects;

/// <summary>
/// نگاشت خالص (بدون وابستگی خارجی، بدون دسترسی به دیتابیس) از مقدار رشته‌ای
/// <c>TB_VAHED_TYPE.TYPECODE</c> به <see cref="VahedCategory"/>. منطق کسب‌وکار است، نه جزئیات
/// دیتابیس — به همین دلیل در <c>Accounting.Domain</c> است، نه <c>Accounting.Infrastructure</c>؛
/// Repository صرفاً <c>TYPECODE</c> را از دیتابیس می‌خواند و این تابع را صدا می‌زند، هرگز آن را
/// دوباره پیاده نمی‌کند.
///
/// وفادار به <c>VahedInfoRepository.GetTypeBimehDarmanAsync</c> در پروژهٔ مرجع
/// (<c>D:\CentralAccount\Infrastructure.Persistance.EF\Repositories\VahedInfoRepository.cs</c>):
/// <list type="bullet">
/// <item><description><c>TYPECODE</c> در {۱ (EdareKol), ۹ (Shob)} → <see cref="VahedCategory.Insurance"/>.</description></item>
/// <item><description><c>TYPECODE</c> در {۲, ۳, ۴, ۵, ۶, ۷, ۸, ۱۵, ۱۶} (Refrans, Bimarestan,
/// PoliCilinic, Darmangah, Deecli, Zayeshgah, SetadDarmangah, Modiriyat, TebKar) →
/// <see cref="VahedCategory.Treatment"/>.</description></item>
/// <item><description>هر مقدار دیگر → <see cref="VahedCategory.All"/> (شاخهٔ <c>else</c> پروژهٔ
/// مرجع).</description></item>
/// </list>
///
/// <b>انحراف عمدی از پروژهٔ مرجع:</b> پروژهٔ مرجع وقتی واحد فراخوان اصلاً پیدا نشود یا
/// <c>TYPECODE</c> قابل‌تفسیر نباشد NRE می‌دهد؛ اینجا هر دو حالت — پارامتر <see langword="null"/>
/// (واحد یافت نشد) و رشتهٔ غیرقابل‌تبدیل به عدد — عمداً به همان مقصد شاخهٔ <c>else</c> یعنی
/// <see cref="VahedCategory.All"/> هدایت می‌شوند، نه throw. رجوع به
/// <c>TafsiliLookupReadRepository</c> برای محل واقعی که این حالت رخ می‌دهد.
/// </summary>
public static class VahedCategoryMapper
{
    private static readonly HashSet<int> InsuranceTypeCodes = new() { 1, 9 };

    private static readonly HashSet<int> TreatmentTypeCodes = new() { 2, 3, 4, 5, 6, 7, 8, 15, 16 };

    /// <summary>
    /// نگاشت <paramref name="typeCode"/> (مقدار خام <c>TB_VAHED_TYPE.TYPECODE</c>) به
    /// <see cref="VahedCategory"/>. هرگز throw نمی‌کند — <see langword="null"/>، رشتهٔ خالی، یا هر
    /// رشتهٔ غیرقابل‌تبدیل به عدد صحیح، همگی به <see cref="VahedCategory.All"/> نگاشت می‌شوند.
    /// </summary>
    public static VahedCategory FromTypeCode(string? typeCode)
    {
        if (int.TryParse(typeCode, out var numericTypeCode))
        {
            if (InsuranceTypeCodes.Contains(numericTypeCode))
            {
                return VahedCategory.Insurance;
            }

            if (TreatmentTypeCodes.Contains(numericTypeCode))
            {
                return VahedCategory.Treatment;
            }
        }

        return VahedCategory.All;
    }
}
