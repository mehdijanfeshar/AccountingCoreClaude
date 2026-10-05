using Accounting.Domain.Entity;
using Accounting.Domain.ValueObjects;
using FluentValidation;

namespace Accounting.Application.CheckBooks;

/// <summary>
/// اوراق چک — عین <c>CheckBook.AddCheckPapers</c> پروژهٔ مرجع: به‌ازای هر شماره از «اولین برگ» تا «آخرین برگ» یک
/// ردیف <c>TB_CHECK</c> با همان طول (صفرپیشرو)، چاپ‌نشده و ابطال‌نشده. ریسک ۲-ج («دسته‌چک بدون برگ چک») را می‌بندد.
/// قواعد مرجع: تغییر بازهٔ شماره پس از ساخت اوراق ممنوع؛ حذف دسته‌چکی که برگی از آن استفاده شده ممنوع.
/// </summary>
public static class CheckBookLeaves
{
    /// <summary>سقف برگ در یک دسته‌چک (جلوگیری از بازهٔ اشتباهی که هزاران ردیف بسازد).</summary>
    public const int MaxLeaves = 1000;

    /// <summary>طول <c>TB_CHECK.CHEQ_NO</c>.</summary>
    public const int MaxNumberLength = 10;

    public static int Generate(TB_CHECKBOOK book, IReadOnlyCollection<string> existingNumbers, string userId, DateTime now)
    {
        var from = long.Parse(book.FROMCHECKNUMBER);
        var to = long.Parse(book.TOCHECKNUMBER);
        var width = book.FROMCHECKNUMBER.Length;
        var existing = existingNumbers.ToHashSet(StringComparer.Ordinal);
        var added = 0;
        for (var n = from; n <= to; n++)
        {
            var no = n.ToString().PadLeft(width, '0');
            if (existing.Contains(no))
                continue;
            book.TB_CHECKs.Add(new TB_CHECK
            {
                ID = Guid.NewGuid(),
                CHECKBOOK_ID = book.ID,
                CHEQ_NO = no,
                EBTAL = CheckCancelStatus.NotCanceled,
                PRINT = CheckPrintStatus.None,
                VAHEDCODE = book.VAHEDCODE,
                ADDUSERID = userId,
                CREATEDDATE = now,
                ISDELETED = false,
            });
            added++;
        }

        return added;
    }

    /// <summary>تعداد شمارهٔ هر دسته‌چک صوری (۰۰۰۱ تا ۱۰۰۰).</summary>
    public const int SoriCapacity = 1000;

    /// <summary>
    /// چک صوری (اعلامیه صوری) — عین <c>Account.AddChechBookSori</c> مرجع: دسته‌چک بدون برگ ساخته می‌شود و بازه‌اش
    /// «سال + کد واحد + ۰۰۰۱» تا «سال + کد واحد + ۱۰۰۰» است (مثلاً ۱۱۵۵ در ۱۴۰۵: 140511550001 تا 140511551000).
    /// برگ فقط وقتی ساخته می‌شود که در سند به کار رود (<c>VoucherChequeService</c>).
    /// </summary>
    public static bool IsSori(CheckType? type) => type == CheckType.Sori;

    public static (string From, string To) SoriRange(string year, string vahedCode)
        => ($"{year}{vahedCode}0001", $"{year}{vahedCode}{SoriCapacity:0000}");

    /// <summary>شمارهٔ بعدی دسته‌چک صوری؛ null = دسته پر شده است. <paramref name="maxUsed"/> = بزرگ‌ترین شمارهٔ موجود (حتی حذف‌شده، به‌خاطر <c>UK_CHECK</c>).</summary>
    public static string? NextSoriNumber(TB_CHECKBOOK book, string? maxUsed)
    {
        var next = maxUsed is null ? long.Parse(book.FROMCHECKNUMBER) : long.Parse(maxUsed) + 1;
        return next > long.Parse(book.TOCHECKNUMBER) ? null : next.ToString().PadLeft(book.FROMCHECKNUMBER.Length, '0');
    }

    /// <summary>برگ استفاده‌شده: تاریخ، تاریخ وصول، بابت، در وجه یا چاپ (عین مرجع).</summary>
    public static bool IsUsed(TB_CHECK c)
        => c.CHEQ_DATE != null || c.DATE_RSID != null || c.PAPER_DESC != null || c.PAYTO != null
           || c.PRINT == CheckPrintStatus.Printed;

    /// <summary>قواعد بازهٔ شماره در Validatorهای ثبت و ویرایش دسته‌چک.</summary>
    public static void RangeRules<T>(AbstractValidator<T> v, Func<T, string?> from, Func<T, string?> to)
    {
        v.RuleFor(x => from(x)).Matches("^[0-9]+$").WithMessage("شمارهٔ اولین برگ فقط رقم است.")
            .MaximumLength(MaxNumberLength).WithMessage($"شمارهٔ برگ حداکثر {MaxNumberLength} رقم است.")
            .OverridePropertyName("FromCheckNumber");
        v.RuleFor(x => to(x)).Matches("^[0-9]+$").WithMessage("شمارهٔ آخرین برگ فقط رقم است.")
            .MaximumLength(MaxNumberLength).WithMessage($"شمارهٔ برگ حداکثر {MaxNumberLength} رقم است.")
            .OverridePropertyName("ToCheckNumber");
        v.RuleFor(x => x)
            .Must(x => from(x)!.Length == to(x)!.Length)
            .WithMessage("طول شمارهٔ اولین برگ با آخرین برگ برابر نیست.")
            .Must(x => long.Parse(from(x)!) <= long.Parse(to(x)!))
            .WithMessage("شمارهٔ اولین برگ نباید بزرگ‌تر از آخرین برگ باشد.")
            .Must(x => long.Parse(to(x)!) - long.Parse(from(x)!) + 1 <= MaxLeaves)
            .WithMessage($"یک دسته‌چک حداکثر {MaxLeaves} برگ دارد.")
            .When(x => from(x) is { Length: > 0 and <= MaxNumberLength } f && to(x) is { Length: > 0 and <= MaxNumberLength } t
                       && f.All(char.IsAsciiDigit) && t.All(char.IsAsciiDigit));
    }
}
