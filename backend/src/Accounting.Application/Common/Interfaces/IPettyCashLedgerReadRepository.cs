using Accounting.Application.PettyCash.Queries;

namespace Accounting.Application.Common.Interfaces;

/// <summary>
/// Read-side repository backing <c>GET api/petty-cash/funds/{fundId}/ledger</c> — بخش ۳-الف
/// (<c>docs/tankhah-khazaneh-module.md</c>). See <see cref="PettyCashFundLedgerDto"/> XML doc for
/// the opening-balance/"real money movement, not the §2 formula" design decisions.
/// </summary>
public interface IPettyCashLedgerReadRepository
{
    /// <param name="from">شمسی YYYYMMDD، یا <see langword="null"/> (بدون کف بازه).</param>
    /// <param name="to">شمسی YYYYMMDD، یا <see langword="null"/> (بدون سقف بازه).</param>
    /// <param name="type">فیلتر نمایشی — <c>"replenishment"</c>/<c>"expense"</c>/<c>"refund"</c>،
    /// یا <see langword="null"/> (همه). فقط ردیف‌های نمایش‌داده‌شده/جمع‌ها را محدود می‌کند —
    /// <c>OpeningBalance</c>/<c>ClosingBalance</c>/هر <c>Balance</c> همیشه از <b>کل</b> تاریخچه
    /// حساب می‌شوند.</param>
    /// <returns><see langword="null"/> when the fund does not exist.</returns>
    Task<PettyCashFundLedgerDto?> GetAsync(
        Guid fundId, string? from, string? to, string? type, string vahedCode, CancellationToken cancellationToken = default);
}
