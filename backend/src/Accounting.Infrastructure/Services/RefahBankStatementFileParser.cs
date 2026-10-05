using Accounting.Application.Common.BankDisk;
using Accounting.Application.Common.Interfaces;
using Accounting.Infrastructure.Legacy;
using Microsoft.EntityFrameworkCore;

namespace Accounting.Infrastructure.Services;

/// <summary>
/// دیسکت بانک رفاه برای «مغایرت بانکی» خزانه‌داری — قالب در <see cref="RefahDiskFormat"/> (مشترک با
/// «کارت حساب جاری»). شمارهٔ چک/فیش در <c>BANK_REFERENCE</c> می‌نشیند تا تطبیق خودکار با شمارهٔ
/// چک/فیش دفتر انجام شود؛ اعلامیهٔ بانک «اعلا-NN». مانده در این قالب نیست.
/// </summary>
public sealed class RefahBankStatementFileParser : IBankStatementFileParser
{
    private readonly LegacyDbContext _dbContext;

    public RefahBankStatementFileParser(LegacyDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyList<ParsedBankStatementLine>> ParseAsync(
        Stream content, Guid bankAccountId, string? fileName, CancellationToken cancellationToken = default)
    {
        RefahDiskFormat.EnsureFileName(fileName);

        var accountNumber = await _dbContext.TB_ACCOUNTs
            .AsNoTracking()
            .Where(a => a.ID == bankAccountId)
            .Select(a => a.ACCOUNTNUMBER)
            .FirstOrDefaultAsync(cancellationToken);

        var records = await RefahDiskFormat.ParseAsync(content, accountNumber, cancellationToken);
        var noticeNo = 0;
        return records
            .Select(r =>
            {
                var (reference, description) = r.DocType switch
                {
                    RefahDiskDocType.BankNotice => ($"اعلا-{++noticeNo:D2}", r.IsDeposit ? "اعلامیهٔ بستانکار بانک" : "اعلامیهٔ بدهکار بانک"),
                    RefahDiskDocType.Fish => (r.Number, "فیش واریزی"),
                    _ => (r.Number, "چک"),
                };
                return new ParsedBankStatementLine(
                    r.Date,
                    reference,
                    description,
                    Withdrawal: r.IsDeposit ? 0 : r.Amount,
                    Deposit: r.IsDeposit ? r.Amount : 0,
                    Balance: null);
            })
            .ToList();
    }
}
