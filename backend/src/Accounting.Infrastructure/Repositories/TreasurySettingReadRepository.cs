using Accounting.Application.Common.Interfaces;
using Accounting.Application.Treasury.Queries;
using Accounting.Infrastructure.Legacy;
using Microsoft.EntityFrameworkCore;

namespace Accounting.Infrastructure.Repositories;

public sealed class TreasurySettingReadRepository : ITreasurySettingReadRepository
{
    private readonly LegacyDbContext _dbContext;

    public TreasurySettingReadRepository(LegacyDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<TreasurySettingDto?> GetByVahedAsync(string vahedCode, CancellationToken cancellationToken = default)
    {
        var setting = await _dbContext.TB_TR_SETTINGs
            .AsNoTracking()
            .Where(s => s.VAHEDCODE == vahedCode && !s.ISDELETED)
            .Select(s => new
            {
                s.ID,
                s.CEO_APPROVAL_THRESHOLD,
                s.BULK_APPROVE_LIMIT,
                s.BENEFICIARY_TAFSIL_GROUP_ID,
                s.PAYABLES_ACCOUNT_ID,
                s.VAT_CREDIT_ACCOUNT_ID,
                s.INSURANCE_PAYABLE_ACCOUNT_ID,
                s.RECEIVABLES_ACCOUNT_ID,
                s.CUSTOMER_TAFSIL_GROUP_ID,
                s.DAILY_TRANSFER_LIMIT,
                s.BANK_FEE_ACCOUNT_ID,
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (setting is null)
        {
            return null;
        }

        string? groupCode = null;
        string? groupName = null;

        if (setting.BENEFICIARY_TAFSIL_GROUP_ID is { } groupId)
        {
            var group = await _dbContext.TB_TAFSIL_GROUPs
                .AsNoTracking()
                .Where(g => g.ID == groupId)
                .Select(g => new { g.TAFSILGROUP_CODE, g.TAFSILGROUP_NAME })
                .FirstOrDefaultAsync(cancellationToken);

            groupCode = group?.TAFSILGROUP_CODE;
            groupName = group?.TAFSILGROUP_NAME;
        }

        var (payablesCode, payablesName) = await GetAccountCodeLabelAsync(setting.PAYABLES_ACCOUNT_ID, cancellationToken);
        var (vatCode, vatName) = await GetAccountCodeLabelAsync(setting.VAT_CREDIT_ACCOUNT_ID, cancellationToken);
        var (insuranceCode, insuranceName) = await GetAccountCodeLabelAsync(setting.INSURANCE_PAYABLE_ACCOUNT_ID, cancellationToken);
        var (receivablesCode, receivablesName) = await GetAccountCodeLabelAsync(setting.RECEIVABLES_ACCOUNT_ID, cancellationToken);
        var (bankFeeCode, bankFeeName) = await GetAccountCodeLabelAsync(setting.BANK_FEE_ACCOUNT_ID, cancellationToken);

        string? customerGroupCode = null;
        string? customerGroupName = null;

        if (setting.CUSTOMER_TAFSIL_GROUP_ID is { } customerGroupId)
        {
            var customerGroup = await _dbContext.TB_TAFSIL_GROUPs
                .AsNoTracking()
                .Where(g => g.ID == customerGroupId)
                .Select(g => new { g.TAFSILGROUP_CODE, g.TAFSILGROUP_NAME })
                .FirstOrDefaultAsync(cancellationToken);

            customerGroupCode = customerGroup?.TAFSILGROUP_CODE;
            customerGroupName = customerGroup?.TAFSILGROUP_NAME;
        }

        return new TreasurySettingDto(
            setting.ID,
            setting.CEO_APPROVAL_THRESHOLD,
            setting.BULK_APPROVE_LIMIT,
            setting.BENEFICIARY_TAFSIL_GROUP_ID,
            groupCode,
            groupName,
            setting.PAYABLES_ACCOUNT_ID,
            payablesCode,
            payablesName,
            setting.VAT_CREDIT_ACCOUNT_ID,
            vatCode,
            vatName,
            setting.INSURANCE_PAYABLE_ACCOUNT_ID,
            insuranceCode,
            insuranceName,
            setting.RECEIVABLES_ACCOUNT_ID,
            receivablesCode,
            receivablesName,
            setting.CUSTOMER_TAFSIL_GROUP_ID,
            customerGroupCode,
            customerGroupName,
            setting.DAILY_TRANSFER_LIMIT,
            setting.BANK_FEE_ACCOUNT_ID,
            bankFeeCode,
            bankFeeName);
    }

    private async Task<(string? Code, string? Name)> GetAccountCodeLabelAsync(Guid? accountCodeId, CancellationToken cancellationToken)
    {
        if (accountCodeId is not { } id)
        {
            return (null, null);
        }

        var account = await _dbContext.TB_ACCOUNTCODEs
            .AsNoTracking()
            .Where(a => a.ID == id)
            .Select(a => new { a.ACCCODE, a.ACCCODENAME })
            .FirstOrDefaultAsync(cancellationToken);

        return (account?.ACCCODE, account?.ACCCODENAME);
    }
}
