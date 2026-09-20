namespace Accounting.Domain.ValueObjects;

/// <summary>
/// نوع درآمدِ اعلامیه — نگاشت مقدار عددی ستون <c>TB_ELAMHEAD.ELAMHDRAMAD_TYPE</c> (که تا
/// ۲۰۲۶-۰۹-۲۰ به‌اشتباه <c>bool?</c> اسکفولد شده بود، یعنی مقدار سوم اصلاً از طریق API قابل
/// نوشتن نبود؛ رجوع به ریسک باز #۲ در <c>CLAUDE.md</c>).
///
/// ⚠️⚠️ <b>برچسب‌ها قطعی نیستند — فقط ترتیب پروژهٔ مرجع است.</b> این یکی از پنج موردِ §۱۰-۴ سند
/// <c>docs/centralaccount-business-reference.md</c> است که کامنت ستون Oracle با enum کد
/// اجراشونده در تناقض است، و اینجا تناقض یک <b>جایگشت کامل سه‌تایی</b> است:
/// <list type="table">
///   <item>
///     <term>کامنت Oracle</term>
///     <description>« 3حق بيمه نوع اعلاميه 1ذي حسابي 2سايردرآمد» ⇒ ۱=ذی‌حسابی، ۲=سایر درآمد، ۳=حق بیمه</description>
///   </item>
///   <item>
///     <term><c>Entities\Elam\DaramElamhType.cs</c> مرجع</term>
///     <description><c>PersonalDramadElam=1</c> (حق بیمه کارکنان)، <c>ZeeDramadElam=2</c> (ذیحسابی)، <c>OtherDramadElam=3</c> (سایر اعلامیه صادره درآمد)</description>
///   </item>
/// </list>
///
/// <b>ترتیب پروژهٔ مرجع انتخاب شد</b>، بر پایهٔ دو سابقه: (۱) نتیجه‌گیری روش‌شناختی §۱۰-۴ که
/// «کامنت‌های ستون Oracle در این schema منبع قابل‌اعتمادی نیستند — حداقل ۵ مورد با کد اجراشونده
/// در تناقض‌اند»، و (۲) تصمیم صریح صاحب پروژه در ۲۰۲۶-۰۹-۱۶ دربارهٔ <c>TB_TAFSILI.OWNER</c> که
/// دقیقاً همین شکل تناقض را به نفع پروژهٔ مرجع حل کرد (رجوع به <see cref="Owners"/>).
///
/// <b>هزینهٔ اشتباه فقط برچسب است، نه داده:</b> مقادیر عددی ذخیره‌شده تغییری نمی‌کنند؛ اگر این
/// ترتیب غلط باشد، صرفاً UI نام نادرست را کنار عدد درست نشان می‌دهد. با این حال تأیید صاحب پروژه
/// لازم است و تا آن لحظه به‌عنوان مورد باز در <c>docs/open-decisions.md</c> ثبت است.
/// </summary>
public enum DaramElamhType
{
    /// <summary>حق بیمه کارکنان.</summary>
    PersonalDramadElam = 1,

    /// <summary>ذی‌حسابی.</summary>
    ZeeDramadElam = 2,

    /// <summary>سایر اعلامیه صادرهٔ درآمد.</summary>
    OtherDramadElam = 3,
}
