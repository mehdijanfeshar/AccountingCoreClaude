using Accounting.Application.Common.Exceptions;
using Accounting.Application.Vouchers.Commands.Common;
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

        var vahedTypeId = await _db.TB_VAHED_INFOs
            .Where(v => v.VAHEDCODE == vahedCode)
            .Select(v => (Guid?)v.VAHEDTYPE_ID)
            .FirstOrDefaultAsync(cancellationToken);

        var rows = await _db.TB_WHITEANDBLACKLISTs
            .Where(r => ids.Contains(r.ACCOUNTCODE_ID)
                        && r.ISDELETED != true
                        && (r.VAHEDTYPE_ID == null || r.VAHEDTYPE_ID == vahedTypeId))
            .Select(r => new
            {
                r.ACCOUNTCODE_ID,
                r.STATE,
                r.FROMAUTHORIZEDDATE,
                r.TOAUTHORIZEDDATE,
                r.FROMLIMITATIONDATE,
                r.TOLIMITATIONDATE,
            })
            .ToListAsync(cancellationToken);

        foreach (var id in ids)
        {
            var mine = rows.Where(r => r.ACCOUNTCODE_ID == id).ToList();
            string? reason = null;

            if (mine.Any(r => r.STATE == WhiteBlackListState.Blacklisted))
            {
                reason = "برای نوع واحد شما در لیست سیاه است";
            }
            else
            {
                var systemOnlyNow = mine.Any(r => r.STATE == WhiteBlackListState.SystemOnly
                                                  && InRange(dateDoc, r.FROMLIMITATIONDATE, r.TOLIMITATIONDATE));
                var allowedNow = mine.Any(r => r.STATE == WhiteBlackListState.Allowed
                                               && InRange(dateDoc, r.FROMAUTHORIZEDDATE, r.TOAUTHORIZEDDATE));

                if (!systemEntry && systemOnlyNow)
                {
                    reason = "فقط سیستمی است و آرتیکل دستی روی آن مجاز نیست";
                }
                else if (!allowedNow && !(systemEntry && systemOnlyNow))
                {
                    reason = "برای نوع واحد شما در این تاریخ مجاز نشده است (ماتریس دسترسی کدینگ)";
                }
            }

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
}
