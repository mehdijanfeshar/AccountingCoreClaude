using Accounting.Domain.Entity;
using Accounting.Domain.ValueObjects;

namespace Accounting.Application.Common.Interfaces;

/// <summary>صورتحساب ماه (DDL 074): آمار اسناد/اعلامیه‌های ماه به تفکیک واحد، تأیید دائم دسته‌ای، لاگ.</summary>
public interface IMonthCloseRepository
{
    /// <summary>تعداد اسناد فعال ماه (<c>DATE_DOC</c> = yyyyMM…) به تفکیک واحد و وضعیت، برای همهٔ واحدها.</summary>
    Task<IReadOnlyList<UnitDocLifeCount>> GetVoucherCountsAsync(string year, int month, CancellationToken cancellationToken = default);

    /// <summary>تعداد اعلامیه‌های صادرهٔ ارسال‌نشده (درآمد ۱..۳، سایر ۵..۷) ماه به تفکیک واحد.</summary>
    Task<IReadOnlyDictionary<string, int>> GetUnsentElamCountsAsync(string year, int month, CancellationToken cancellationToken = default);

    /// <summary>اسناد «بررسی‌شده» ماه این واحدها ⇒ «تأیید دائم». بدون stage؛ مستقیم اجرا می‌شود (داخل تراکنش فراخوان).</summary>
    Task<int> AcceptReviewedAsync(string year, int month, IReadOnlyCollection<string> vahedCodes, string userId, CancellationToken cancellationToken = default);

    Task AddLogsAsync(IEnumerable<TB_MONTH_CLOSE_LOG> logs, CancellationToken cancellationToken = default);

    /// <summary>لاگ سال (و ماه اختیاری)؛ <paramref name="vahedCode"/> null = همهٔ واحدها.</summary>
    Task<IReadOnlyList<TB_MONTH_CLOSE_LOG>> GetLogsAsync(string year, int? month, string? vahedCode, CancellationToken cancellationToken = default);
}

public sealed record UnitDocLifeCount(string VahedCode, DocLife? DocLife, int Count);
