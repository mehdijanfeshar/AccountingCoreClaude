using Accounting.Application.ChequeBook;
using Accounting.Application.Common;
using Accounting.Domain.Entity;

namespace Accounting.Application.Common.Interfaces;

/// <summary>دفتر چک — چک‌های <c>TB_CHECK</c> که در ردیف سند به کار رفته‌اند، و کارتابل تأییدشان.</summary>
public interface IChequeBookRepository
{
    /// <summary>چک tracked؛ چک واحد دیگر ⇒ ۴۰۳، نبود ⇒ null.</summary>
    Task<TB_CHECK?> GetCheckForUpdateAsync(Guid checkId, string vahedCode, CancellationToken cancellationToken = default);

    /// <summary>آیا چک در ردیف سند فعال دیگری (جز <paramref name="excludeDetailId"/>) به کار رفته است؟</summary>
    Task<bool> IsUsedByOtherDetailAsync(Guid checkId, Guid? excludeDetailId, CancellationToken cancellationToken = default);

    /// <summary>ردیف سند فعالی که این چک را دارد؛ null = هنوز در سند نیامده.</summary>
    Task<TB_VOUCHERSDETAIL?> GetActiveDetailAsync(Guid checkId, CancellationToken cancellationToken = default);

    /// <summary>دسته‌چک‌های صوری حساب‌های بانکی همین معین، با شمارهٔ بعدی.</summary>
    Task<IReadOnlyList<SoriChequeBookDto>> GetSoriBooksAsync(
        string vahedCode, Guid? accountCodeId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<AvailableChequeDto>> GetAvailableAsync(
        string vahedCode, Guid? accountCodeId, string? search, CancellationToken cancellationToken = default);

    Task<PagedResult<ChequeBookItemDto>> GetPagedAsync(ChequeBookFilter filter, CancellationToken cancellationToken = default);

    Task<TB_CHECK_APPROVAL?> GetApprovalForUpdateAsync(Guid checkId, CancellationToken cancellationToken = default);

    Task AddApprovalAsync(TB_CHECK_APPROVAL approval, CancellationToken cancellationToken = default);

    Task AddApprovalEventAsync(TB_CHECK_APPROVAL_EVENT approvalEvent, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ChequeApprovalEventDto>> GetEventsAsync(Guid checkId, CancellationToken cancellationToken = default);

    Task<ChequePrintDto?> GetPrintDataAsync(Guid checkId, CancellationToken cancellationToken = default);

    /// <summary>اوراق یک دسته‌چک به ترتیب شماره، با سند و وضعیت کارتابل هر برگ.</summary>
    Task<IReadOnlyList<ChequeLeafDto>> GetLeavesAsync(Guid checkBookId, CancellationToken cancellationToken = default);
}
