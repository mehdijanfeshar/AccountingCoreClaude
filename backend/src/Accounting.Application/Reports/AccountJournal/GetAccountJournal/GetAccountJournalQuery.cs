using Accounting.Application.Common.Security;
using MediatR;
using System.Text.Json.Serialization;

namespace Accounting.Application.Reports.AccountJournal.GetAccountJournal;

/// <summary>
/// دفتر روزنامه — the chronological journal: every posting line in the selected range, in voucher
/// order. READ-ONLY.
///
/// <para>
/// Paged for the same reason as مرور اسناد: this is a list of individual lines, not an aggregate,
/// so a page is a genuine subset — and the totals that must not be partial come back separately in
/// <see cref="AccountJournalResultDto"/>.
/// </para>
/// </summary>
/// <param name="PageNumber">1-based page number.</param>
/// <param name="PageSize">Page size, capped by <see cref="GetAccountJournalQueryValidator.MaxPageSize"/>.</param>
/// <param name="Year">سال مالی — required exact match.</param>
/// <param name="FromVoucherNo">Optional inclusive lower bound on شماره سند.</param>
/// <param name="ToVoucherNo">Optional inclusive upper bound on شماره سند.</param>
/// <param name="FromDate">Optional inclusive lower bound on تاریخ سند (Jalali <c>YYYYMMDD</c>).</param>
/// <param name="ToDate">Optional inclusive upper bound on تاریخ سند.</param>
/// <param name="FromAccountCode">Optional inclusive lower bound on کد معین.</param>
/// <param name="ToAccountCode">Optional inclusive upper bound on کد معین.</param>
/// <param name="DocLife">Optional exact وضعیت سند filter.</param>
/// <param name="Description">Optional «contains» filter on شرح ردیف.</param>
public sealed record GetAccountJournalQuery(
    int PageNumber,
    int PageSize,
    string Year,
    string? FromVoucherNo = null,
    string? ToVoucherNo = null,
    string? FromDate = null,
    string? ToDate = null,
    string? FromAccountCode = null,
    string? ToAccountCode = null,
    int? DocLife = null,
    string? Description = null) : IRequest<AccountJournalResultDto>, IVahedScopedQuery
{
    /// <summary>
    /// Server-assigned by <c>VahedScopeBehavior</c> from the caller's effective unit — never bound
    /// from client input. The view carries <c>VAHEDCODE</c>, so without this the journal would
    /// interleave every unit's postings.
    /// </summary>
    [JsonIgnore]
    public string VahedCode { get; set; } = string.Empty;
}
