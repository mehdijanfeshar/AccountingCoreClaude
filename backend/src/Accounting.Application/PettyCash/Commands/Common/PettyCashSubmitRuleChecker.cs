using Accounting.Application.Common.Exceptions;
using Accounting.Application.Common.Interfaces;

namespace Accounting.Application.PettyCash.Commands.Common;

public sealed class PettyCashSubmitRuleChecker : IPettyCashSubmitRuleChecker
{
    private readonly IPettyCashExpenseDocRepository _expenseDocRepository;

    public PettyCashSubmitRuleChecker(IPettyCashExpenseDocRepository expenseDocRepository)
    {
        _expenseDocRepository = expenseDocRepository;
    }

    public async Task EnsureSubmittableAsync(
        Guid expenseDocId,
        Guid fundId,
        decimal fundCeiling,
        decimal perDocLimit,
        string? invoiceDate,
        string year,
        decimal totalAmount,
        string vahedCode,
        Guid? excludeDocId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(invoiceDate) || !invoiceDate.StartsWith(year, StringComparison.Ordinal))
        {
            throw new PettyCashInvoiceYearMismatchException(expenseDocId, invoiceDate, year);
        }

        if (totalAmount > perDocLimit)
        {
            throw new PettyCashPerDocLimitExceededException(expenseDocId, totalAmount, perDocLimit);
        }

        var exposure = await _expenseDocRepository.GetFundExposureAsync(
            fundId, vahedCode, excludeDocId, cancellationToken);

        var cashBalance = fundCeiling - exposure.ApprovedAmount - exposure.InFlightAmount;

        if (totalAmount > cashBalance)
        {
            throw new PettyCashInsufficientCashBalanceException(expenseDocId, totalAmount, cashBalance);
        }
    }
}
