using Accounting.Domain.Entity;

namespace Accounting.Application.Common.Interfaces;

/// <summary>رمزهای برگشت صورتحساب ماه (DDL 073) و اسناد تأیید دائم یک ماه. فقط stage می‌کند.</summary>
public interface IMonthReopenRepository
{
    Task AddAsync(TB_MONTH_REOPEN row, CancellationToken cancellationToken = default);

    /// <summary>تعداد همهٔ رمزهای صادرشده برای (واحد، سال، ماه) — شمارهٔ دفعهٔ بعد = این + ۱.</summary>
    Task<int> CountAsync(string vahedCode, string year, int month, CancellationToken cancellationToken = default);

    /// <summary>آخرین رمز مصرف‌نشده و باطل‌نشده (change-tracked)، یا null.</summary>
    Task<TB_MONTH_REOPEN?> GetOpenForUpdateAsync(string vahedCode, string year, int month, int maxFailedAttempts, CancellationToken cancellationToken = default);

    /// <summary>سابقهٔ یک سال؛ <paramref name="vahedCode"/> null = همهٔ واحدها.</summary>
    Task<IReadOnlyList<TB_MONTH_REOPEN>> GetForYearAsync(string year, string? vahedCode, CancellationToken cancellationToken = default);

    /// <summary>اسناد فعال «تأیید دائم» واحد در ماه (از روی <c>DATE_DOC</c> = yyyyMMdd)، change-tracked.</summary>
    Task<IReadOnlyList<TB_VOUCHERSHEAD>> GetAcceptedVouchersForUpdateAsync(string vahedCode, string year, int month, CancellationToken cancellationToken = default);
}

/// <summary>
/// تولید رمز برگشت: عدد ۸ رقمی از HMAC-SHA256 روی (واحد، سال، ماه، دفعه) با کلید محرمانهٔ
/// <c>MonthReopen:Secret</c>. سمت برگشت همان را دوباره می‌سازد و مقایسه می‌کند.
/// </summary>
public interface IMonthReopenCodeGenerator
{
    string Generate(string vahedCode, string year, int month, int seq);
}
