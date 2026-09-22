using Accounting.Application.Reports.AccountJournal;
using Accounting.Application.Reports.AccountJournal.GetAccountJournal;

namespace Accounting.Application.Common.Interfaces;

/// <summary>
/// Read side of دفتر روزنامه.
///
/// <para>
/// ⚠️ Backed by TB_VOUCHERSDETAIL, not by the view that exists for it — a recorded exception to
/// team working-rule #2, because <c>VW_ACCOUNTJOURNALREPORT</c> never excludes deleted vouchers
/// and collapses several lines of one معین into a single row. The full argument is on
/// <c>AccountJournalReadRepository</c>.
/// </para>
/// </summary>
public interface IAccountJournalReadRepository
{
    /// <summary>
    /// Returns one page of posting lines matching the query, in voucher order, together with the
    /// بدهکار/بستانکار totals of the entire filtered set — not of the returned page.
    /// </summary>
    Task<AccountJournalResultDto> GetAsync(
        GetAccountJournalQuery query,
        CancellationToken cancellationToken = default);
}
