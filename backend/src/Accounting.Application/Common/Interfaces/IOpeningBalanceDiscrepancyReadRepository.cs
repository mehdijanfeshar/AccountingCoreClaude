using Accounting.Application.Reports.OpeningBalanceDiscrepancy;

namespace Accounting.Application.Common.Interfaces;

public interface IOpeningBalanceDiscrepancyReadRepository
{
    /// <summary>ردیف‌های واقعی سند(های) افتتاحیهٔ <paramref name="year"/> — ردیف رابط افتتاحیه
    /// (<paramref name="openingInterfaceAccountId"/>) کنار گذاشته می‌شود — به تفکیک معین و شناسه (اگر معین
    /// شناسه‌دار باشد).</summary>
    Task<IReadOnlyList<OpeningBalanceAccountLine>> GetOpeningLinesAsync(
        string vahedCode, string year, Guid openingInterfaceAccountId, CancellationToken cancellationToken = default);

    /// <summary>ماندهٔ کل <paramref name="year"/> (همهٔ اسناد، بدون فیلتر گروه) به تفکیک معین و شناسه —
    /// برای مقایسه با سند افتتاحیهٔ سال بعد («مانده سال قبل»).</summary>
    Task<IReadOnlyList<OpeningBalanceAccountLine>> GetYearBalancesByAttributeAsync(
        string vahedCode, string year, CancellationToken cancellationToken = default);
}
