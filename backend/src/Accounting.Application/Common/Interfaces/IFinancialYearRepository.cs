using Accounting.Domain.Entity;

namespace Accounting.Application.Common.Interfaces;

/// <summary>
/// نوشتن سال مالی تازه (<c>TB_YEAR</c>) و دسته‌چک‌های صوری سال تازه — «ایجاد سال مالی» ستاد مرکزی.
/// </summary>
public interface IFinancialYearRepository
{
    /// <summary>همهٔ ردیف‌های <c>TB_YEAR</c> (چند ده ردیف)، ردیابی‌شده برای تغییر <c>ISCURRENT</c>.</summary>
    Task<List<TB_YEAR>> GetAllForUpdateAsync(CancellationToken cancellationToken = default);

    void Add(TB_YEAR year);

    /// <summary>حساب‌های بانکی فعال همهٔ واحدها: (شناسه، کد واحد). حذف‌شده و بی‌واحد کنار گذاشته می‌شوند.</summary>
    Task<IReadOnlyList<(Guid AccountId, string VahedCode)>> GetActiveBankAccountsAsync(CancellationToken cancellationToken = default);

    /// <summary>دسته‌چک‌های صوری (با حذف‌شده‌ها) که شمارهٔ اولشان با <paramref name="year"/> شروع می‌شود، ردیابی‌شده.</summary>
    Task<List<TB_CHECKBOOK>> GetSoriCheckBooksOfYearForUpdateAsync(string year, CancellationToken cancellationToken = default);

    void AddCheckBook(TB_CHECKBOOK checkBook);
}
