using Accounting.Application.Common.Security;
using Accounting.Domain.Entity;
using Accounting.Infrastructure.Legacy;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace Accounting.Infrastructure.Repositories;

/// <summary>
/// جدول <c>TB_ROLE_MENU_ACCESS</c> (DDL 075) با کش ۳۰ ثانیه‌ای — هر درخواست نوشتنی (RoleAuthorizationBehavior) آن را
/// می‌خواند. تا DDL اجرا نشده (ORA-00942) یا جدول خالی است، null برمی‌گرداند ⇒ رفتار ثابت قبلی.
/// </summary>
public sealed class RoleMenuAccessStore : IRoleMenuAccessStore
{
    private const string CacheKey = "role-menu-access-snapshot";
    private static readonly TimeSpan CacheFor = TimeSpan.FromSeconds(30);

    private readonly LegacyDbContext _dbContext;
    private readonly IMemoryCache _cache;

    public RoleMenuAccessStore(LegacyDbContext dbContext, IMemoryCache cache)
    {
        _dbContext = dbContext;
        _cache = cache;
    }

    private sealed record Cached(bool Available, RoleMenuAccessSnapshot? Snapshot);

    public async Task<RoleMenuAccessSnapshot?> GetSnapshotAsync(CancellationToken cancellationToken = default)
        => (await LoadAsync(cancellationToken)).Snapshot;

    public async Task<bool> IsAvailableAsync(CancellationToken cancellationToken = default)
        => (await LoadAsync(cancellationToken)).Available;

    private async Task<Cached> LoadAsync(CancellationToken cancellationToken)
    {
        if (_cache.TryGetValue(CacheKey, out Cached? cached) && cached is not null)
            return cached;

        Cached result;
        try
        {
            var rows = await _dbContext.TB_ROLE_MENU_ACCESSes.AsNoTracking()
                .Select(r => new { r.ROLE_NAME, r.MENU_KEY, r.ACCESS_LEVEL })
                .ToListAsync(cancellationToken);
            result = new Cached(true, rows.Count == 0
                ? null
                : new RoleMenuAccessSnapshot(rows.Select(r => (r.ROLE_NAME, r.MENU_KEY, r.ACCESS_LEVEL))));
        }
        catch (Exception ex) when (IsMissingTable(ex))
        {
            result = new Cached(false, null);
        }

        _cache.Set(CacheKey, result, CacheFor);
        return result;
    }

    public async Task SaveRoleAsync(
        string role, IReadOnlyDictionary<string, int> levels, string userId, CancellationToken cancellationToken = default)
    {
        var existing = await _dbContext.TB_ROLE_MENU_ACCESSes
            .Where(r => r.ROLE_NAME == role)
            .ToListAsync(cancellationToken);
        var byKey = existing.ToDictionary(r => r.MENU_KEY, StringComparer.Ordinal);
        var now = DateTime.UtcNow;

        foreach (var (key, level) in levels)
        {
            if (byKey.TryGetValue(key, out var row))
            {
                if (row.ACCESS_LEVEL == level)
                    continue;
                row.ACCESS_LEVEL = level;
                row.CHANGEUSERID = userId;
                row.UPDATEDDATE = now;
            }
            else
            {
                _dbContext.TB_ROLE_MENU_ACCESSes.Add(new TB_ROLE_MENU_ACCESS
                {
                    ID = Guid.NewGuid(),
                    ROLE_NAME = role,
                    MENU_KEY = key,
                    ACCESS_LEVEL = level,
                    ADDUSERID = userId,
                    CREATEDDATE = now,
                });
            }
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
        _cache.Remove(CacheKey);
    }

    public async Task DeleteRoleAsync(string role, CancellationToken cancellationToken = default)
    {
        var rows = await _dbContext.TB_ROLE_MENU_ACCESSes.Where(r => r.ROLE_NAME == role).ToListAsync(cancellationToken);
        _dbContext.TB_ROLE_MENU_ACCESSes.RemoveRange(rows);
        await _dbContext.SaveChangesAsync(cancellationToken);
        _cache.Remove(CacheKey);
    }

    private static bool IsMissingTable(Exception ex)
    {
        for (var e = ex; e is not null; e = e.InnerException)
        {
            if (e.Message.Contains("ORA-00942", StringComparison.Ordinal)
                || e.Message.Contains("no such table", StringComparison.OrdinalIgnoreCase))
                return true;
        }
        return false;
    }
}
