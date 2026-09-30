using System.Text.Json;

namespace Accounting.Application.FinancialStatements;

/// <summary>نوع خط زیر/بالای ردیف در برگهٔ صورت (سند منبع §۴-۱).</summary>
public enum FsBorder
{
    None = 0,
    Single = 1,
    Double = 2,
}

/// <summary>
/// قالب‌بندی نمایش یک ردیف قالب صورت مالی — به‌صورت JSON در <c>TB_FS_TEMPLATE_ROW.FORMAT_JSON</c>
/// ذخیره می‌شود. همهٔ فیلدها اختیاری‌اند؛ <see langword="null"/> در ستون یعنی پیش‌فرض.
/// </summary>
/// <param name="Indent">تورفتگی (۰ تا ۵).</param>
/// <param name="Bold">پررنگ (معمولاً جمع‌ها).</param>
/// <param name="Italic">کج.</param>
/// <param name="TopBorder">خط بالای مقدار (جمع جزء).</param>
/// <param name="BottomBorder">خط زیر مقدار (<see cref="FsBorder.Double"/> زیر جمع نهایی).</param>
/// <param name="HideIfZero">اگر در همهٔ ستون‌ها صفر بود، ردیف نمایش داده نشود.</param>
/// <param name="PageBreakBefore">در چاپ، پیش از این ردیف صفحهٔ جدید.</param>
/// <param name="InnerColumn">مقدار در ستون داخلی (اقلام) نمایش داده شود، نه ستون بیرونی (جمع).</param>
public sealed record FsRowFormat(
    int Indent = 0,
    bool Bold = false,
    bool Italic = false,
    FsBorder TopBorder = FsBorder.None,
    FsBorder BottomBorder = FsBorder.None,
    bool HideIfZero = false,
    bool PageBreakBefore = false,
    bool InnerColumn = false)
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public static FsRowFormat Default { get; } = new();

    public static string? ToJson(FsRowFormat? format)
        => format is null || format == Default ? null : JsonSerializer.Serialize(format, JsonOptions);

    public static FsRowFormat FromJson(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return Default;
        }

        try
        {
            return JsonSerializer.Deserialize<FsRowFormat>(json, JsonOptions) ?? Default;
        }
        catch (JsonException)
        {
            return Default;
        }
    }
}
