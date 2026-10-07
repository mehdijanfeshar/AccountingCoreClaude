using System.Security.Cryptography;
using System.Text;
using Accounting.Application.Common.Exceptions;
using Accounting.Application.Common.Interfaces;
using Accounting.Domain.Entity;
using Accounting.Domain.ValueObjects;
using Accounting.Infrastructure.Legacy;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace Accounting.Infrastructure.Repositories;

/// <summary><see cref="IMonthReopenRepository"/> روی <c>TB_MONTH_REOPEN</c> (DDL 073) و <c>TB_VOUCHERSHEAD</c>.</summary>
public sealed class MonthReopenRepository : IMonthReopenRepository
{
    private readonly LegacyDbContext _db;

    public MonthReopenRepository(LegacyDbContext db)
    {
        _db = db;
    }

    public async Task AddAsync(TB_MONTH_REOPEN row, CancellationToken cancellationToken = default)
        => await _db.TB_MONTH_REOPENs.AddAsync(row, cancellationToken);

    public Task<int> CountAsync(string vahedCode, string year, int month, CancellationToken cancellationToken = default)
        => _db.TB_MONTH_REOPENs.CountAsync(r => r.VAHEDCODE == vahedCode && r.YEAR == year && r.MONTH == month, cancellationToken);

    public Task<TB_MONTH_REOPEN?> GetOpenForUpdateAsync(string vahedCode, string year, int month, int maxFailedAttempts, CancellationToken cancellationToken = default)
        => _db.TB_MONTH_REOPENs
            .Where(r => r.VAHEDCODE == vahedCode && r.YEAR == year && r.MONTH == month
                        && r.USEDDATE == null && r.FAILED_ATTEMPTS < maxFailedAttempts)
            .OrderByDescending(r => r.SEQ)
            .FirstOrDefaultAsync(cancellationToken);

    public async Task<IReadOnlyList<TB_MONTH_REOPEN>> GetForYearAsync(string year, string? vahedCode, CancellationToken cancellationToken = default)
    {
        var q = _db.TB_MONTH_REOPENs.AsNoTracking().Where(r => r.YEAR == year);
        if (vahedCode is not null)
            q = q.Where(r => r.VAHEDCODE == vahedCode);
        return await q.OrderByDescending(r => r.ISSUEDDATE).ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<TB_VOUCHERSHEAD>> GetAcceptedVouchersForUpdateAsync(string vahedCode, string year, int month, CancellationToken cancellationToken = default)
    {
        // DATE_DOC = yyyyMMdd (VARCHAR2(8)). Prefix match on year+month of the document date.
        var prefix = $"{year}{month:00}";
        return await _db.TB_VOUCHERSHEADs
            .Where(h => h.VAHEDCODE == vahedCode && h.YEAR == year && h.ISDELETED != true
                        && h.DOCLIFE == DocLife.Accepted && h.DATE_DOC != null && h.DATE_DOC.StartsWith(prefix))
            .ToListAsync(cancellationToken);
    }
}

/// <summary>
/// رمز ۸ رقمی: HMAC-SHA256(کلید <c>MonthReopen:Secret</c>، "واحد|سال|ماه|دفعه") → ۴ بایت اول →
/// باقی‌ماندهٔ تقسیم بر ۱۰^۸. بدون کلید، صدور و برگشت هر دو با پیام روشن رد می‌شوند.
/// </summary>
public sealed class MonthReopenCodeGenerator : IMonthReopenCodeGenerator
{
    public const string SecretKey = "MonthReopen:Secret";

    private readonly IConfiguration _configuration;

    public MonthReopenCodeGenerator(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public string Generate(string vahedCode, string year, int month, int seq)
    {
        var secret = _configuration[SecretKey];
        if (string.IsNullOrWhiteSpace(secret) || secret.Trim().Length < 16)
            throw new MonthReopenException("کلید رمز برگشت صورتحساب (MonthReopen:Secret، دست‌کم ۱۶ نویسه) در تنظیمات سرور تعریف نشده است.");

        var message = Encoding.UTF8.GetBytes($"{vahedCode.Trim()}|{year.Trim()}|{month:00}|{seq}");
        var hash = HMACSHA256.HashData(Encoding.UTF8.GetBytes(secret.Trim()), message);
        var value = ((uint)hash[0] << 24 | (uint)hash[1] << 16 | (uint)hash[2] << 8 | hash[3]) % 100_000_000u;
        return value.ToString("D8");
    }
}
