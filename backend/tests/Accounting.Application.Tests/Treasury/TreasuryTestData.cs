using Accounting.Application.Queries.Treasury;

namespace Accounting.Application.Tests.Treasury;

/// <summary>سازندهٔ مشترک دادهٔ تست خزانه‌داری.</summary>
internal static class TreasuryTestData
{
    public static TreasurySettingDto Setting(
        decimal ceoThreshold = 1_000m,
        decimal bulkLimit = 500m,
        decimal? dailyLimit = null,
        Guid? beneficiaryGroupId = null,
        Guid? payablesAccountId = null,
        Guid? bankFeeAccountId = null) => new(
            Id: Guid.NewGuid(),
            CeoApprovalThreshold: ceoThreshold,
            BulkApproveLimit: bulkLimit,
            BeneficiaryTafsilGroupId: beneficiaryGroupId,
            BeneficiaryTafsilGroupCode: null,
            BeneficiaryTafsilGroupName: null,
            PayablesAccountId: payablesAccountId,
            PayablesAccountCode: null,
            PayablesAccountName: null,
            VatCreditAccountId: null,
            VatCreditAccountCode: null,
            VatCreditAccountName: null,
            InsurancePayableAccountId: null,
            InsurancePayableAccountCode: null,
            InsurancePayableAccountName: null,
            ReceivablesAccountId: null,
            ReceivablesAccountCode: null,
            ReceivablesAccountName: null,
            CustomerTafsilGroupId: null,
            CustomerTafsilGroupCode: null,
            CustomerTafsilGroupName: null,
            DailyTransferLimit: dailyLimit,
            BankFeeAccountId: bankFeeAccountId,
            BankFeeAccountCode: null,
            BankFeeAccountName: null);
}
