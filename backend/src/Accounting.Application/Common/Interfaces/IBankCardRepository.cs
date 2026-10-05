using Accounting.Application.BankCards;
using Accounting.Domain.Entity;

namespace Accounting.Application.Common.Interfaces;

/// <summary>
/// کارت حساب جاری (عملیات) — روی <c>TB_BANKCARTDETAIL</c> سیستم قدیم، تا داده‌های قدیمی هم دیده شوند.
/// همه‌چیز stage؛ ذخیره با UnitOfWork.
/// </summary>
public interface IBankCardRepository
{
    /// <summary>حساب بانکی واحد؛ حساب واحد دیگر ⇒ ۴۰۳، نبود ⇒ null.</summary>
    Task<BankCardAccount?> GetAccountAsync(Guid bankAccountId, string vahedCode, CancellationToken cancellationToken = default);

    /// <summary>ردیف‌های فعال کارت یک حساب در سال؛ <paramref name="month"/> null = همهٔ ماه‌ها. tracked.</summary>
    Task<IReadOnlyList<TB_BANKCARTDETAIL>> GetRowsAsync(
        string vahedCode, string year, string accountNumber, string? month, CancellationToken cancellationToken = default);

    Task<TB_BANKCARTDETAIL?> GetRowForUpdateAsync(Guid id, string vahedCode, CancellationToken cancellationToken = default);

    Task AddRowAsync(TB_BANKCARTDETAIL row, CancellationToken cancellationToken = default);

    /// <summary>
    /// چک‌های دسته‌چک‌های همین حساب بانکی که ردیف سندی با همین شماره و مبلغ بستانکار در واحد و سال دارند.
    /// </summary>
    Task<IReadOnlyList<BankCardMatch>> FindChequesAsync(
        Guid bankAccountId, string vahedCode, string year, IReadOnlyCollection<string> numbers,
        CancellationToken cancellationToken = default);

    /// <summary>فیش/حواله‌هایی با این شماره که ردیف سند بدهکار روی معین بانک در واحد و سال دارند.</summary>
    Task<IReadOnlyList<BankCardMatch>> FindReceiptsAsync(
        Guid accountCodeId, string vahedCode, string year, IReadOnlyCollection<string> numbers,
        CancellationToken cancellationToken = default);

    /// <summary>تاریخ وصول را روی چک/فیش می‌نویسد (عین مرجع: <c>SetDateResid</c>). null = پاک کردن.</summary>
    Task SetCheckReceivedDateAsync(Guid checkId, string? date, CancellationToken cancellationToken = default);

    Task SetReceiptReceivedDateAsync(Guid receiptId, string? date, CancellationToken cancellationToken = default);

    /// <summary>
    /// ردیف‌های سند روی معین و تفصیلی‌های حساب بانکی تا <paramref name="toDate"/> در سال که چک/فیش دارند
    /// ولی آن چک/فیش در هیچ ردیف کارت مغایرت‌گیری نشده.
    /// </summary>
    Task<IReadOnlyList<BankCardBookItemDto>> GetOutstandingBookItemsAsync(
        Guid accountCodeId, IReadOnlyCollection<Guid> tafsiliIds, string vahedCode, string year, string toDate,
        CancellationToken cancellationToken = default);
}
