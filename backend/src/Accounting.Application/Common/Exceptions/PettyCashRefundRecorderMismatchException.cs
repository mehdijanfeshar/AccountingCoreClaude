using Accounting.Domain.ValueObjects;

namespace Accounting.Application.Common.Exceptions;

/// <summary>
/// Thrown by <c>CreatePettyCashRefundCommandHandler</c>/<c>DeletePettyCashRefundCommandHandler</c>
/// when the caller does not hold the role/identity <c>TB_PC_FUND.REFUND_RECORDER</c> designates
/// for this specific تنخواه — بخش ۳-الف (<c>docs/tankhah-khazaneh-module.md</c>، صفحهٔ ۱۱
/// پاورپوینت): «ثبت استرداد فقط برای کاربری که نقش متناظر (یا برای ۱، CUSTODIAN_USERID) همان
/// تنخواه را دارد».
///
/// <b>403, not 409</b> — a straight permissions gap.
/// </summary>
public sealed class PettyCashRefundRecorderMismatchException : Exception
{
    public PettyCashRefundRecorderMismatchException(Guid fundId, PettyCashRefundRecorder requiredRecorder)
        : base($"Caller is not the designated {requiredRecorder} recorder for petty-cash fund {fundId}.")
    {
        FundId = fundId;
        RequiredRecorder = requiredRecorder;
    }

    public Guid FundId { get; }

    public PettyCashRefundRecorder RequiredRecorder { get; }

    public string PublicDetail => "شما مجاز به ثبت استرداد وجه برای این تنخواه نیستید.";
}
