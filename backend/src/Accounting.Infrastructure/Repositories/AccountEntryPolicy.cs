using Accounting.Application.Common.Exceptions;
using Accounting.Application.Commands.Vouchers.Common;
using Accounting.Domain.ValueObjects;
using Accounting.Infrastructure.Legacy;
using Microsoft.EntityFrameworkCore;

namespace Accounting.Infrastructure.Repositories;

/// <summary><see cref="IAccountEntryPolicy"/> روی <c>TB_WHITEANDBLACKLIST</c>.</summary>
public sealed class AccountEntryPolicy : IAccountEntryPolicy
{
    private readonly LegacyDbContext _db;

    public AccountEntryPolicy(LegacyDbContext db)
    {
        _db = db;
    }

    public Task EnsureManualEntryAllowedAsync(
        string vahedCode,
        string? dateDoc,
        IEnumerable<Guid?> accountIds,
        CancellationToken cancellationToken = default)
        => EnsureAsync(vahedCode, dateDoc, accountIds, systemEntry: false, cancellationToken);

    public Task EnsureSystemEntryAllowedAsync(
        string vahedCode,
        string? dateDoc,
        IEnumerable<Guid?> accountIds,
        CancellationToken cancellationToken = default)
        => EnsureAsync(vahedCode, dateDoc, accountIds, systemEntry: true, cancellationToken);

    public async Task<IReadOnlySet<Guid>> GetManualEntryAllowedIdsAsync(
        string vahedCode,
        string? dateDoc,
        CancellationToken cancellationToken = default)
    {
        // همهٔ ردیف‌های ماتریس برای این نوع واحد (بدون فیلتر روی حساب) — پیش‌فرض ممنوع است، پس هر معینی
        // که ردیف ندارد اینجا نیست و در لیست انتخاب هم نمی‌آید.
        var rows = await LoadRowsAsync(vahedCode, null, cancellationToken);
        return rows
            .GroupBy(r => r.AccountCodeId)
            .Where(g => Reason(g, dateDoc, systemEntry: false) is null)
            .Select(g => g.Key)
            .ToHashSet();
    }

    private async Task EnsureAsync(
        string vahedCode,
        string? dateDoc,
        IEnumerable<Guid?> accountIds,
        bool systemEntry,
        CancellationToken cancellationToken)
    {
        var ids = accountIds.Where(i => i is not null).Select(i => i!.Value).Distinct().ToList();
        if (ids.Count == 0)
        {
            return;
        }

        var rows = await LoadRowsAsync(vahedCode, ids, cancellationToken);

        foreach (var id in ids)
        {
            var reason = Reason(rows.Where(r => r.AccountCodeId == id), dateDoc, systemEntry);
            if (reason is not null)
            {
                var code = await _db.TB_ACCOUNTCODEs
                    .Where(a => a.ID == id)
                    .Select(a => a.ACCCODE + " - " + a.ACCCODENAME)
                    .FirstOrDefaultAsync(cancellationToken);
                throw new BusinessRuleException($"معین «{code ?? id.ToString()}» {reason}.");
            }
        }
    }

    private async Task<List<MatrixRow>> LoadRowsAsync(string vahedCode, List<Guid>? ids, CancellationToken cancellationToken)
    {
        var vahedTypeId = await _db.TB_VAHED_INFOs
            .Where(v => v.VAHEDCODE == vahedCode)
            .Select(v => (Guid?)v.VAHEDTYPE_ID)
            .FirstOrDefaultAsync(cancellationToken);

        var query = _db.TB_WHITEANDBLACKLISTs
            .Where(r => r.ISDELETED != true && (r.VAHEDTYPE_ID == null || r.VAHEDTYPE_ID == vahedTypeId));
        if (ids is not null)
        {
            query = query.Where(r => ids.Contains(r.ACCOUNTCODE_ID));
        }

        return await query
            .Select(r => new MatrixRow(
                r.ACCOUNTCODE_ID,
                r.STATE,
                r.FROMAUTHORIZEDDATE,
                r.TOAUTHORIZEDDATE,
                r.FROMLIMITATIONDATE,
                r.TOLIMITATIONDATE))
            .ToListAsync(cancellationToken);
    }

    /// <summary>دلیل ممنوع بودن یک معین با ردیف‌های ماتریسش، یا null اگر مجاز است.</summary>
    private static string? Reason(IEnumerable<MatrixRow> rows, string? dateDoc, bool systemEntry)
    {
        var mine = rows.ToList();
        if (mine.Any(r => r.State == WhiteBlackListState.Blacklisted))
        {
            return "برای نوع واحد شما در لیست سیاه است";
        }

        var systemOnlyNow = mine.Any(r => r.State == WhiteBlackListState.SystemOnly
                                          && InRange(dateDoc, r.FromLimitation, r.ToLimitation));
        var allowedNow = mine.Any(r => r.State == WhiteBlackListState.Allowed
                                       && InRange(dateDoc, r.FromAuthorized, r.ToAuthorized));

        if (!systemEntry && systemOnlyNow)
        {
            return "فقط سیستمی است و آرتیکل دستی روی آن مجاز نیست";
        }

        if (!allowedNow && !(systemEntry && systemOnlyNow))
        {
            return "برای نوع واحد شما در این تاریخ مجاز نشده است (ماتریس دسترسی کدینگ)";
        }

        return null;
    }

    /// <summary>تاریخ‌ها <c>yyyyMMdd</c> هم‌طول‌اند، پس مقایسهٔ رشته‌ای کافی است. سر خالی = بی‌انتها.</summary>
    private static bool InRange(string? date, string? from, string? to)
    {
        if (string.IsNullOrWhiteSpace(date))
        {
            return string.IsNullOrWhiteSpace(from) && string.IsNullOrWhiteSpace(to);
        }

        return (string.IsNullOrWhiteSpace(from) || string.CompareOrdinal(date, from) >= 0)
            && (string.IsNullOrWhiteSpace(to) || string.CompareOrdinal(date, to) <= 0);
    }

    private sealed record MatrixRow(
        Guid AccountCodeId,
        WhiteBlackListState? State,
        string? FromAuthorized,
        string? ToAuthorized,
        string? FromLimitation,
        string? ToLimitation);
}
