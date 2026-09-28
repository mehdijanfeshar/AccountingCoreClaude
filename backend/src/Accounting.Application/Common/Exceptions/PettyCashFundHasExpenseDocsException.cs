namespace Accounting.Application.Common.Exceptions;

/// <summary>
/// Thrown by <c>DeletePettyCashFundCommandHandler</c> when the target <c>TB_PC_FUND</c> still has
/// at least one non-deleted <c>TB_PC_EXPENSE_DOC</c> row referencing it. 409: the fund itself is a
/// valid delete target, it just conflicts with live child rows — same shape as
/// <c>PettyCashDuplicateExpenseDocException</c>.
/// </summary>
public sealed class PettyCashFundHasExpenseDocsException : Exception
{
    public PettyCashFundHasExpenseDocsException(Guid fundId)
        : base($"Petty-cash fund {fundId} still has non-deleted expense documents.")
    {
        FundId = fundId;
    }

    public Guid FundId { get; }

    public string PublicDetail => "این تنخواه دارای صورت‌هزینهٔ حذف‌نشده است و قابل حذف نیست.";
}
