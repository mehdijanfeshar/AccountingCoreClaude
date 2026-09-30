namespace Accounting.Domain.ValueObjects;

/// <summary>
/// نوع صورت — <c>TB_FS_TEMPLATE.STATEMENT_TYPE</c>. فاز ۴۵ (<c>docs/fs-module.md</c> §۲).
/// </summary>
public enum FsStatementType
{
    /// <summary>صورت خالص دارایی‌های در دسترس برای پرداخت مزایا (طرح بیمه‌ای).</summary>
    NetAssets = 1,
    /// <summary>صورت تغییرات در خالص دارایی‌ها (طرح بیمه‌ای).</summary>
    ChangesInNetAssets = 2,
    /// <summary>گردش حساب ارزش ویژه (طرح بیمه‌ای).</summary>
    EquityMovement = 3,
    /// <summary>صورت وضعیت مالی.</summary>
    FinancialPosition = 4,
    /// <summary>صورت سود و زیان.</summary>
    ProfitOrLoss = 5,
    /// <summary>صورت سود و زیان جامع.</summary>
    ComprehensiveIncome = 6,
    /// <summary>صورت تغییرات در حقوق مالکانه.</summary>
    ChangesInEquity = 7,
    /// <summary>صورت جریان‌های نقدی.</summary>
    CashFlow = 8,
    /// <summary>صورت عملکرد مالی (بخش عمومی).</summary>
    FinancialPerformance = 9,
    /// <summary>گزارش مدیریتی/آزاد.</summary>
    Custom = 10,
    /// <summary>
    /// یادداشت عددی صورت‌ها — به یک ردیف صورت وصل است (<c>TB_FS_TEMPLATE.NOTE_PARENT_*</c>)، شماره‌اش
    /// در اجرا خودکار داده می‌شود و «عنوان»های داخلش زیر‌یادداشت‌اند (بخش ۴۵-ج).
    /// </summary>
    Note = 11,
}
