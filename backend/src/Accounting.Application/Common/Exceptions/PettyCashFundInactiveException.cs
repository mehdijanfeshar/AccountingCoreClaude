namespace Accounting.Application.Common.Exceptions;

/// <summary>
/// Thrown by <c>CreatePettyCashExpenseDocCommandHandler</c> and
/// <c>SubmitPettyCashExpenseDocCommandHandler</c> when the target <c>TB_PC_FUND.IS_ACTIVE</c> is
/// <see langword="false"/> — an inactive تنخواه accepts no new صورت‌هزینه and none may be
/// submitted against it (2026-09-28 decision, <c>docs/tankhah-khazaneh-module.md</c> §0). 409: the
/// request is well-formed, it just conflicts with the fund's current (deactivated) state — same
/// shape as <c>PettyCashDocNotEditableException</c>.
/// </summary>
public sealed class PettyCashFundInactiveException : Exception
{
    public PettyCashFundInactiveException(Guid fundId)
        : base($"Petty-cash fund {fundId} is inactive.")
    {
        FundId = fundId;
    }

    public Guid FundId { get; }

    public string PublicDetail => "این تنخواه غیرفعال است و امکان ثبت یا ارسال صورت‌هزینهٔ جدید روی آن وجود ندارد.";
}
