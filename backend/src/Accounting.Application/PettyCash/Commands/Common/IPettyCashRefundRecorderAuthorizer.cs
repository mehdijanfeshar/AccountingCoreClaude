using Accounting.Domain.Entity;

namespace Accounting.Application.PettyCash.Commands.Common;

/// <summary>
/// The single place «آیا کاربر جاری مجاز به ثبت/حذف استرداد وجه این تنخواه است؟» lives — shared by
/// <c>CreatePettyCashRefundCommandHandler</c> and <c>DeletePettyCashRefundCommandHandler</c>, so
/// the <c>TB_PC_FUND.REFUND_RECORDER</c> check cannot drift apart between them (بخش ۳-الف،
/// <c>docs/tankhah-khazaneh-module.md</c>).
/// </summary>
public interface IPettyCashRefundRecorderAuthorizer
{
    /// <exception cref="Accounting.Application.Common.Exceptions.PettyCashRefundRecorderMismatchException">
    /// Thrown when the caller does not hold the role/identity <c>fund.REFUND_RECORDER</c>
    /// designates.</exception>
    Task EnsureCanRecordAsync(TB_PC_FUND fund, CancellationToken cancellationToken = default);
}
