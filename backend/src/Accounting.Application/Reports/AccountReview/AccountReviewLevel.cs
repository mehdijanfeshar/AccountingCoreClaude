namespace Accounting.Application.Reports.AccountReview;

/// <summary>
/// Which level مرور حساب‌ها aggregates by — the report's single most important input.
///
/// <para>
/// Mirrors the reference project's <c>typeShow</c> (1..10) exactly, including the ordering, so the
/// two systems produce comparable output for the same selection. The first three come from the
/// account coding hierarchy; the last seven are the تفصیلی levels, which the view has already
/// flattened into <c>TAFSILICODE1..7</c> — that flattening is why one report can pivot across all
/// ten without a different join per level.
/// </para>
/// </summary>
public enum AccountReviewLevel
{
    /// <summary>گروه.</summary>
    Group = 1,

    /// <summary>کل.</summary>
    Kol = 2,

    /// <summary>معین.</summary>
    Moin = 3,

    /// <summary>تفصیلی سطح ۱.</summary>
    Tafsili1 = 4,

    /// <summary>تفصیلی سطح ۲.</summary>
    Tafsili2 = 5,

    /// <summary>تفصیلی سطح ۳.</summary>
    Tafsili3 = 6,

    /// <summary>تفصیلی سطح ۴.</summary>
    Tafsili4 = 7,

    /// <summary>تفصیلی سطح ۵.</summary>
    Tafsili5 = 8,

    /// <summary>تفصیلی سطح ۶.</summary>
    Tafsili6 = 9,

    /// <summary>تفصیلی سطح ۷.</summary>
    Tafsili7 = 10,

    // ── تفکیک واحد (درخواست صاحب پروژه ۲۰۲۶-۱۰-۱۱): خانوادهٔ دوم، مستقل از کدینگ. هر حساب را می‌توان به استان،
    // گروه واحد (بیمه‌ای/درمانی/ستادی)، واحد و در نهایت سند شکست و با همان مسیر برگشت. کد مرحله: کد استان،
    // شمارهٔ گروه (1/2/3)، کد واحد. «سند» فقط سطح نمایش است (مرحلهٔ مسیر نمی‌شود) و یک واحد مشخص می‌خواهد.

    /// <summary>استان (از شهر واحد).</summary>
    Province = 11,

    /// <summary>گروه واحد — بیمه‌ای/درمانی/ستادی.</summary>
    UnitCategory = 12,

    /// <summary>واحد.</summary>
    Unit = 13,

    /// <summary>سند — فقط در یک واحد.</summary>
    Voucher = 14,
}

/// <summary>دو خانوادهٔ سطح مرور حساب‌ها.</summary>
public static class AccountReviewLevels
{
    /// <summary>استان / گروه واحد / واحد / سند.</summary>
    public static bool IsUnitDimension(AccountReviewLevel level) => level >= AccountReviewLevel.Province;
}
