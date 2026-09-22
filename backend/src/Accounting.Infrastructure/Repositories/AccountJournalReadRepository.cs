using Accounting.Application.Common.Interfaces;
using Accounting.Application.Reports.AccountJournal;
using Accounting.Application.Reports.AccountJournal.GetAccountJournal;
using Accounting.Domain.Entity;
using Accounting.Infrastructure.Legacy;
using Microsoft.EntityFrameworkCore;

namespace Accounting.Infrastructure.Repositories;

/// <summary>
/// EF Core (Oracle) implementation of <see cref="IAccountJournalReadRepository"/> — دفتر روزنامه.
///
/// <para>
/// <b>⚠️ Recorded exception to team working-rule #2 ("the read side reads from a View").</b> The
/// view <c>VW_ACCOUNTJOURNALREPORT</c> exists and this repository was first written against it.
/// Its definition, read off live Oracle (2026-09-22), rules it out on two counts:
/// </para>
///
/// <list type="number">
/// <item>
/// <b>It never excludes deleted vouchers.</b> Its join is a plain
/// <c>JOIN TB_VOUCHERSHEAD vh</c> with no <c>isdeleted</c> predicate, and it does not select that
/// column either — so soft-deleted vouchers appear in the journal and there is no column left to
/// filter them out with. Every other read path in this project excludes them.
/// </item>
/// <item>
/// <b>It is not a line-level journal.</b> It groups by <c>(voucher, معین, head_desc)</c> and sums,
/// so two lines posted to the same معین within one voucher collapse into a single row, and the
/// شرح shown is the voucher's, never the line's. A دفتر روزنامه is by definition the sequence of
/// individual postings.
/// </item>
/// </list>
///
/// <para>
/// Compare <see cref="MatrixReportReadRepository"/>, which does follow rule #2: its view keeps one
/// row per line and filters <c>isdeleted = 0</c> in the join. The rule is sound; this particular
/// view is not.
/// </para>
///
/// <para>
/// So this reads <c>TB_VOUCHERSDETAIL</c> directly, one row per posting line, with its own شرح and
/// its own معین — and excludes deleted lines and deleted vouchers, which is the behaviour every
/// other report in this project already has.
/// </para>
/// </summary>
public sealed class AccountJournalReadRepository : IAccountJournalReadRepository
{
    /// <summary>
    /// Width of <c>DOC_NUM</c> — see the identical constant on
    /// <see cref="VoucherReviewReadRepository"/> for why range bounds must be padded.
    /// </summary>
    private const int VoucherNumberWidth = 6;

    /// <summary>
    /// Length of a معین code. <c>TB_ACCOUNTCODE.ACCCODE</c> holds the full coded account, and the
    /// معین is its first six characters — the same <c>SUBSTR(AccCode, 1, 6)</c> the reference view
    /// uses. Taken from the reference rather than invented.
    /// </summary>
    private const int MoinCodeLength = 6;

    private readonly LegacyDbContext _dbContext;

    public AccountJournalReadRepository(LegacyDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<AccountJournalResultDto> GetAsync(
        GetAccountJournalQuery query,
        CancellationToken cancellationToken = default)
    {
        var lines = FilterLines(query);

        var summary = await lines
            .GroupBy(_ => 1)
            .Select(g => new
            {
                Count = g.Count(),
                Debtor = g.Sum(d => d.DEBTOR ?? 0m),
                Creditor = g.Sum(d => d.CREDITOR ?? 0m),
            })
            .FirstOrDefaultAsync(cancellationToken);

        var items = await lines
            // Chronological, oldest first — the opposite of مرور اسناد on purpose. A journal is
            // read forwards: it is the ordered record of what happened, and reversing it would
            // make the running sequence of postings unreadable. RADIF keeps a voucher's own lines
            // in the order they were entered.
            .OrderBy(d => d.VOUCHERSHEAD!.DATE_DOC)
            .ThenBy(d => d.VOUCHERSHEAD!.DOC_NUM)
            .ThenBy(d => d.RADIF)
            .ThenBy(d => d.ID)
            .Skip((query.PageNumber - 1) * query.PageSize)
            .Take(query.PageSize)
            .Select(d => new AccountJournalRowDto(
                d.VOUCHERSHEAD!.DOC_NUM ?? string.Empty,
                d.VOUCHERSHEAD!.DATE_DOC ?? string.Empty,
                d.ACCOUNT!.ACCCODE == null
                    ? string.Empty
                    : d.ACCOUNT!.ACCCODE.Substring(0, MoinCodeLength),
                d.ACCOUNT!.ACCCODENAME ?? string.Empty,
                d.DESCRIPTION ?? string.Empty,
                d.VOUCHERSHEAD!.DOCLIFE == null ? null : (int)d.VOUCHERSHEAD!.DOCLIFE!.Value,
                d.DEBTOR ?? 0m,
                d.CREDITOR ?? 0m))
            .ToListAsync(cancellationToken);

        return new AccountJournalResultDto(
            items,
            query.PageNumber,
            query.PageSize,
            summary?.Count ?? 0,
            summary?.Debtor ?? 0m,
            summary?.Creditor ?? 0m);
    }

    private IQueryable<TB_VOUCHERSDETAIL> FilterLines(GetAccountJournalQuery query)
    {
        // Both the line and its voucher must be alive. Checking only the line would let the
        // cascade-deleted lines of a deleted voucher through if the cascade ever missed one, and
        // checking only the head would show lines removed from a surviving voucher.
        var lines = _dbContext.TB_VOUCHERSDETAILs
            .AsNoTracking()
            .Where(d => d.ISDELETED != true)
            .Where(d => d.VOUCHERSHEAD != null && d.VOUCHERSHEAD.ISDELETED != true)
            .Where(d => d.VOUCHERSHEAD!.YEAR == query.Year)
            .Where(d => d.VOUCHERSHEAD!.VAHEDCODE == query.VahedCode);

        if (!string.IsNullOrWhiteSpace(query.FromDate))
        {
            lines = lines.Where(d => d.VOUCHERSHEAD!.DATE_DOC != null
                && string.Compare(d.VOUCHERSHEAD!.DATE_DOC, query.FromDate) >= 0);
        }

        if (!string.IsNullOrWhiteSpace(query.ToDate))
        {
            lines = lines.Where(d => d.VOUCHERSHEAD!.DATE_DOC != null
                && string.Compare(d.VOUCHERSHEAD!.DATE_DOC, query.ToDate) <= 0);
        }

        if (!string.IsNullOrWhiteSpace(query.FromVoucherNo))
        {
            var from = query.FromVoucherNo.PadLeft(VoucherNumberWidth, '0');
            lines = lines.Where(d => d.VOUCHERSHEAD!.DOC_NUM != null
                && string.Compare(d.VOUCHERSHEAD!.DOC_NUM, from) >= 0);
        }

        if (!string.IsNullOrWhiteSpace(query.ToVoucherNo))
        {
            var to = query.ToVoucherNo.PadLeft(VoucherNumberWidth, '0');
            lines = lines.Where(d => d.VOUCHERSHEAD!.DOC_NUM != null
                && string.Compare(d.VOUCHERSHEAD!.DOC_NUM, to) <= 0);
        }

        // ACCCODE is NOT zero-padded the way DOC_NUM is — it is the account as coded, so its
        // bounds are compared as typed. A prefix bound therefore behaves as a prefix: "11" as the
        // lower bound includes "1100…", which is what an accountant means by «از کد ۱۱».
        if (!string.IsNullOrWhiteSpace(query.FromAccountCode))
        {
            var from = query.FromAccountCode;
            lines = lines.Where(d => d.ACCOUNT!.ACCCODE != null
                && string.Compare(d.ACCOUNT!.ACCCODE, from) >= 0);
        }

        if (!string.IsNullOrWhiteSpace(query.ToAccountCode))
        {
            // Upper bound is exclusive of longer codes unless padded: "12" would otherwise exclude
            // "1200…" because "1200" > "12" ordinally. Padding the bound with the highest code
            // character keeps «تا کد ۱۲» meaning "everything under 12".
            var to = query.ToAccountCode.PadRight(MoinCodeLength, '9');
            lines = lines.Where(d => d.ACCOUNT!.ACCCODE != null
                && string.Compare(d.ACCOUNT!.ACCCODE, to) <= 0);
        }

        if (query.DocLife.HasValue)
        {
            var docLife = (Accounting.Domain.ValueObjects.DocLife)query.DocLife.Value;
            lines = lines.Where(d => d.VOUCHERSHEAD!.DOCLIFE == docLife);
        }

        if (!string.IsNullOrWhiteSpace(query.Description))
        {
            var term = query.Description;
            lines = lines.Where(d => d.DESCRIPTION != null && d.DESCRIPTION.Contains(term));
        }

        return lines;
    }
}
