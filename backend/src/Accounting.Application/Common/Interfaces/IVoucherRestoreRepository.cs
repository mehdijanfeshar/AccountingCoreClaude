using Accounting.Domain.Entity;

namespace Accounting.Application.Common.Interfaces;

/// <summary>
/// برگرداندن سند حذف‌شده (حذف نرم). حذف سرسند، ردیف‌ها و لینک‌های تفصیلی را با <b>یک</b> زمان
/// (<c>UPDATEDDATE</c>) حذف می‌کند؛ بازگردانی دقیقاً همان‌هایی را برمی‌گرداند که هم‌زمان با سرسند حذف شده‌اند،
/// نه ردیف‌هایی که پیش‌تر جداگانه پاک شده بودند.
/// </summary>
public interface IVoucherRestoreRepository
{
    Task<IReadOnlyList<DeletedVoucherRow>> ListDeletedAsync(string vahedCode, string year, CancellationToken ct);

    Task<TB_VOUCHERSHEAD?> GetDeletedForRestoreAsync(Guid id, string vahedCode, CancellationToken ct);

    Task<bool> IsDocNumTakenAsync(string vahedCode, string year, string docNum, Guid excludeId, CancellationToken ct);

    /// <summary>ردیف‌ها و لینک‌هایی که با <paramref name="deletedAt"/> حذف شده‌اند را زنده می‌کند (stage؛ ذخیره با handler).</summary>
    Task<int> RestoreTreeAsync(Guid headId, DateTime? deletedAt, string? userId, DateTime now, CancellationToken ct);
}

public sealed record DeletedVoucherRow(
    Guid Id,
    string? DocNum,
    string? DateDoc,
    string? HeadDesc,
    DateTime? DeletedAt,
    string? DeletedBy,
    int LineCount,
    decimal Debtor,
    decimal Creditor);
