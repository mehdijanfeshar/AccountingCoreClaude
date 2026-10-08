using Accounting.Domain.Entity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Accounting.Infrastructure.Legacy;

/// <summary>
/// شمارهٔ عطف سند (<c>TB_VOUCHERSHEAD.ATF_NUM</c>) — تصمیم صاحب پروژه ۲۰۲۶-۱۰-۰۸:
/// <list type="bullet">
/// <item>هنگام <b>ایجاد</b> هر سند، سرور تخصیص می‌دهد: سال (۴) + کد واحد (۴) + شمارهٔ ردیف ۷ رقمی = ۱۵ رقم؛
/// مثلاً واحد ۱۱۵۵ در ۱۴۰۴: <c>140411550000001</c>.</item>
/// <item>شمارهٔ ردیف از بزرگ‌ترین عطف موجودِ همان سال و واحد ادامه می‌یابد — هر دو قالب: جدید (سال+واحد) و
/// قالب سیستم قدیم (واحد+سال، مثل <c>115514040000050</c>). اسناد حذف‌شده هم حساب‌اند؛ شماره هرگز تکرار نمی‌شود.</item>
/// <item>عطف <b>هیچ‌وقت عوض نمی‌شود</b>: نه با ویرایش سند، نه با «مرتب‌سازی» که شمارهٔ سند را بر اساس تاریخ
/// بازنویسی می‌کند. هر تغییر ATF_NUM روی سند موجود بی‌صدا به مقدار قبلی برمی‌گردد.</item>
/// </list>
/// <para>
/// <b>Why an interceptor.</b> Vouchers are created on many paths (voucher form, Hesabyar templates,
/// treasury, petty cash, month close, vouchers received from other systems). One hook on save
/// covers every one of them, including paths added later, and no caller can forget it.
/// </para>
/// <para>
/// ⚠️ Two concurrent saves in the same unit and year can read the same maximum. DDL <c>076</c> adds a
/// unique index on ATF_NUM so the second save fails instead of duplicating a number; until it is
/// applied, a duplicate is possible under concurrent creation.
/// </para>
/// </summary>
public sealed class VoucherAtfNumberInterceptor : SaveChangesInterceptor
{
    public const int SequenceLength = 7;

    public static string Format(string year, string vahedCode, long sequence)
        => year + vahedCode + sequence.ToString("D" + SequenceLength);

    public override async ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
    {
        if (eventData.Context is LegacyDbContext context)
            await ApplyAsync(context, cancellationToken);
        return result;
    }

    public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result)
    {
        if (eventData.Context is LegacyDbContext context)
            ApplyAsync(context, CancellationToken.None).GetAwaiter().GetResult();
        return result;
    }

    private static async Task ApplyAsync(LegacyDbContext context, CancellationToken cancellationToken)
    {
        var entries = context.ChangeTracker.Entries<TB_VOUCHERSHEAD>().ToList();

        foreach (var entry in entries.Where(e => e.State == EntityState.Modified))
            KeepOriginal(entry);

        var added = entries
            .Where(e => e.State == EntityState.Added)
            .Select(e => e.Entity)
            .Where(h => h.YEAR is { Length: 4 } && h.VAHEDCODE is { Length: 4 })
            .GroupBy(h => (Year: h.YEAR!, Vahed: h.VAHEDCODE!));

        foreach (var group in added)
        {
            var next = await MaxSequenceAsync(context, group.Key.Year, group.Key.Vahed, cancellationToken) + 1;
            foreach (var head in group)
                head.ATF_NUM = Format(group.Key.Year, group.Key.Vahed, next++);
        }
    }

    private static void KeepOriginal(EntityEntry<TB_VOUCHERSHEAD> entry)
    {
        var atf = entry.Property(h => h.ATF_NUM);
        if (!atf.IsModified)
            return;
        atf.CurrentValue = atf.OriginalValue;
        atf.IsModified = false;
    }

    /// <summary>Largest 7-digit sequence already used in this year and unit, in either format; 0 if none.</summary>
    private static async Task<long> MaxSequenceAsync(
        LegacyDbContext context, string year, string vahedCode, CancellationToken cancellationToken)
    {
        var newPrefix = year + vahedCode;
        var legacyPrefix = vahedCode + year;

        var max = await context.TB_VOUCHERSHEADs
            .AsNoTracking()
            .Where(h => h.YEAR == year && h.VAHEDCODE == vahedCode && h.ATF_NUM != null
                        && (h.ATF_NUM.StartsWith(newPrefix) || h.ATF_NUM.StartsWith(legacyPrefix)))
            .Select(h => h.ATF_NUM!.Substring(8, SequenceLength))
            .MaxAsync(cancellationToken);

        return long.TryParse(max, out var value) ? value : 0;
    }
}
