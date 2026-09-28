using System.Globalization;
using Accounting.Domain.ValueObjects;

namespace Accounting.Application.PettyCash.Common;

/// <summary>
/// Pure Jalali (شمسی) period-boundary math for تسویهٔ دوره — بخش ۳-ب
/// (<c>docs/tankhah-khazaneh-module.md</c> section 9). No I/O, no repository dependency — same
/// "one rule, one home" shape as <see cref="PettyCashBalanceCalculator"/>, so the preview query
/// and the finalize command can never compute a different period range for the same fund.
///
/// Uses <see cref="System.Globalization.PersianCalendar"/>, the same approach
/// <c>ReverseVoucherCommandHandler.TodayJalali</c> and the بخش ۳-الف validators already use — no
/// shared helper existed to reuse (each call site is a private one-liner), so this is the first
/// place that promotes the pattern to a small, testable static class.
///
/// <b>Periods never look at "today".</b> The current computable period for a fund is always
/// "whatever comes immediately after the last <see cref="PettyCashSettlementState.Final"/>
/// period" (or, if none exists yet, the period containing the fund's own creation date) —
/// never "the month/quarter containing today's date". This is what makes "دوره‌ها باید
/// پشت‌سرهم نهایی شوند" true by construction: a fund whose owner has not finalized in months
/// still has its next period be the very next one after the last Final, not a arbitrary jump to
/// "the current month".
/// </summary>
public static class PettyCashSettlementPeriodCalculator
{
    private static readonly PersianCalendar Calendar = new();

    /// <summary>Formats a <see cref="DateTime"/> (any <see cref="DateTimeKind"/> — only the
    /// calendar date component is used) as شمسی <c>YYYYMMDD</c>.</summary>
    public static string ToJalaliString(DateTime date)
        => $"{Calendar.GetYear(date):0000}{Calendar.GetMonth(date):00}{Calendar.GetDayOfMonth(date):00}";

    /// <summary>Parses a شمسی <c>YYYYMMDD</c> string back into a <see cref="DateTime"/> (Gregorian,
    /// midnight, unspecified kind).</summary>
    public static DateTime ParseJalali(string yyyymmdd)
    {
        var year = int.Parse(yyyymmdd[..4], CultureInfo.InvariantCulture);
        var month = int.Parse(yyyymmdd.Substring(4, 2), CultureInfo.InvariantCulture);
        var day = int.Parse(yyyymmdd.Substring(6, 2), CultureInfo.InvariantCulture);

        return Calendar.ToDateTime(year, month, day, 0, 0, 0, 0);
    }

    /// <summary>
    /// The (start, end) شمسی <c>YYYYMMDD</c> boundaries of the month or quarter (per
    /// <paramref name="mode"/>) that contains <paramref name="date"/>. Quarters are calendar
    /// quarters of the شمسی year: ماه‌های ۱–۳, ۴–۶, ۷–۹, ۱۰–۱۲ — per §۹'s explicit rule, not the
    /// Gregorian quarter <paramref name="date"/> would fall into.
    /// </summary>
    public static (string Start, string End) GetPeriodContaining(PettyCashSettlementPeriod mode, DateTime date)
    {
        var year = Calendar.GetYear(date);
        var month = Calendar.GetMonth(date);

        int startMonth;
        int endMonth;

        if (mode == PettyCashSettlementPeriod.Quarterly)
        {
            var quarterIndex = (month - 1) / 3;
            startMonth = quarterIndex * 3 + 1;
            endMonth = startMonth + 2;
        }
        else
        {
            startMonth = month;
            endMonth = month;
        }

        var start = Calendar.ToDateTime(year, startMonth, 1, 0, 0, 0, 0);
        var daysInEndMonth = Calendar.GetDaysInMonth(year, endMonth);
        var end = Calendar.ToDateTime(year, endMonth, daysInEndMonth, 0, 0, 0, 0);

        return (ToJalaliString(start), ToJalaliString(end));
    }

    /// <summary>
    /// The next period a fund should open, per the "never look at today" rule above.
    /// </summary>
    /// <param name="settlementPeriod"><see cref="Accounting.Domain.Entity.TB_PC_FUND.SETTLEMENT_PERIOD"/> —
    /// <see langword="null"/> means <see cref="PettyCashSettlementPeriod.Monthly"/> (§۹'s
    /// explicit default, mirroring §۱'s existing "دورهٔ تسویه" fund-setting default).</param>
    /// <param name="lastFinalPeriodEnd">The previous <see cref="PettyCashSettlementState.Final"/>
    /// period's <c>PERIOD_END</c> (شمسی <c>YYYYMMDD</c>), or <see langword="null"/> when the fund
    /// has never had a period finalized.</param>
    /// <param name="fundCreatedDateUtc"><c>TB_PC_FUND.CREATEDDATE</c> — anchors the very first
    /// period when <paramref name="lastFinalPeriodEnd"/> is <see langword="null"/>.</param>
    public static (string Start, string End) GetNextPeriod(
        PettyCashSettlementPeriod? settlementPeriod,
        string? lastFinalPeriodEnd,
        DateTime fundCreatedDateUtc)
    {
        var mode = settlementPeriod ?? PettyCashSettlementPeriod.Monthly;

        if (string.IsNullOrEmpty(lastFinalPeriodEnd))
        {
            return GetPeriodContaining(mode, fundCreatedDateUtc);
        }

        var dayAfterLastFinal = ParseJalali(lastFinalPeriodEnd).AddDays(1);

        return GetPeriodContaining(mode, dayAfterLastFinal);
    }
}
