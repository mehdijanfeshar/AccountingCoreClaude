namespace Accounting.Application.Common.Exceptions;

/// <summary>
/// Thrown when a caller who is not <c>TB_PC_FUND.CUSTODIAN_USERID</c> attempts to
/// create/submit/update/delete a صورت‌هزینه against that fund — تکمیل بخش ۲ (۲۰۲۶-۰۹-۲۸، «ثبت سند
/// فقط توسط تنخواه‌دار»، <c>docs/tankhah-khazaneh-module.md</c>). Custodian is not a
/// <c>TB_PC_REVIEWER</c> role; this is a plain field comparison against the fund row, unrelated to
/// <see cref="PettyCashReviewerAccessDeniedException"/>.
///
/// <b>403, not 409</b> — same shape as <see cref="PettyCashReviewerAccessDeniedException"/>: a
/// straight permissions gap, not a state conflict.
/// </summary>
public sealed class PettyCashNotCustodianException : Exception
{
    public PettyCashNotCustodianException(Guid fundId)
        : base($"Caller is not the custodian of petty-cash fund {fundId}.")
    {
        FundId = fundId;
    }

    public Guid FundId { get; }

    public string PublicDetail => "فقط تنخواه‌دار همین تنخواه می‌تواند این عملیات را انجام دهد.";
}
