namespace Accounting.Domain.ValueObjects;

/// <summary>
/// دسته‌بندی واحد سازمانی برای اعمال «قانون ب» دیدن ردیف‌های تفصیلی مجاز
/// (فیلتر <c>TB_TAFSIL_LINK_TAFSILGROUP.VAHEDTYPE</c> در مسیر خواندن تفصیلی‌های قابل انتخاب —
/// رجوع به <c>GetTafsiliLevelItemsQuery</c> برای شرح کامل قانون).
///
/// معادل enum <c>TypeKoli</c> در پروژهٔ مرجع
/// (<c>D:\CentralAccount\Tamin.Core\Entities\Enum\TypeKoli.cs</c>): <c>bimeh = 1</c>,
/// <c>Darman = 2</c>, <c>All = 3</c>. نگاشت مقدار عددی <c>TB_VAHED_TYPE.TYPECODE</c> به این
/// enum در <see cref="VahedCategoryMapper.FromTypeCode"/> است، نه اینجا — این فایل فقط خودِ
/// enum را نگه می‌دارد: POCO خالص، بدون هیچ وابستگی خارجی، مطابق قید صفر-وابستگی
/// <c>Accounting.Domain</c>.
/// </summary>
public enum VahedCategory
{
    /// <summary>بیمه — معادل <c>bimeh = 1</c> در پروژهٔ مرجع.</summary>
    Insurance = 1,

    /// <summary>درمان — معادل <c>Darman = 2</c> در پروژهٔ مرجع.</summary>
    Treatment = 2,

    /// <summary>
    /// همه — معادل <c>All = 3</c> در پروژهٔ مرجع. یک ردیف <c>TB_TAFSIL_LINK_TAFSILGROUP</c> با
    /// <c>VAHEDTYPE == 3</c> برای هر واحدی، صرف‌نظر از دستهٔ آن، قابل مشاهده است.
    /// </summary>
    All = 3,
}
