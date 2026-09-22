namespace Accounting.Application.Reports.AccountJournal;

/// <summary>
/// A page of دفتر روزنامه plus the totals of the <b>whole filtered set</b>.
///
/// <para>
/// Same shape and same reasoning as <c>VoucherReviewResultDto</c>: the field names mirror
/// <c>PagedResult&lt;T&gt;</c> so existing frontend paging reads it unchanged, and the totals
/// cover every matching line rather than the current page. For a journal that matters more than
/// anywhere else — the sum of a page of postings is a number with no accounting meaning at all.
/// </para>
/// </summary>
/// <param name="Items">The requested page, in voucher order.</param>
/// <param name="PageNumber">1-based page number.</param>
/// <param name="PageSize">Requested page size.</param>
/// <param name="TotalCount">Number of posting lines matching the filter.</param>
/// <param name="TotalDebtor">جمع کل بدهکار across every matching line.</param>
/// <param name="TotalCreditor">جمع کل بستانکار across every matching line.</param>
public sealed record AccountJournalResultDto(
    IReadOnlyList<AccountJournalRowDto> Items,
    int PageNumber,
    int PageSize,
    int TotalCount,
    decimal TotalDebtor,
    decimal TotalCreditor);
