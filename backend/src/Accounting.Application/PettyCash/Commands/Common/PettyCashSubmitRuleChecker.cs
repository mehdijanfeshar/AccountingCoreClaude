using Accounting.Application.Common.Exceptions;
using Accounting.Application.Common.Interfaces;
using Accounting.Application.PettyCash.Common;

namespace Accounting.Application.PettyCash.Commands.Common;

public sealed class PettyCashSubmitRuleChecker : IPettyCashSubmitRuleChecker
{
    private readonly IPettyCashExpenseDocRepository _expenseDocRepository;
    private readonly IPettyCashReplenishmentRepository _replenishmentRepository;
    private readonly IPettyCashRefundRepository _refundRepository;

    public PettyCashSubmitRuleChecker(
        IPettyCashExpenseDocRepository expenseDocRepository,
        IPettyCashReplenishmentRepository replenishmentRepository,
        IPettyCashRefundRepository refundRepository)
    {
        _expenseDocRepository = expenseDocRepository;
        _replenishmentRepository = replenishmentRepository;
        _refundRepository = refundRepository;
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

        // بخش ۳-الف: extended equation — see PettyCashBalanceCalculator XML doc.
        var paidReplenishmentTotal = await _replenishmentRepository.GetPaidTotalAsync(fundId, vahedCode, cancellationToken);
        var refundTotal = await _refundRepository.GetTotalAsync(fundId, vahedCode, cancellationToken);

        var cashBalance = PettyCashBalanceCalculator.CashBalance(
            fundCeiling, exposure.ApprovedAmount, exposure.InFlightAmount, paidReplenishmentTotal, refundTotal);

        if (totalAmount > cashBalance)
        {
            throw new PettyCashInsufficientCashBalanceException(expenseDocId, totalAmount, cashBalance);
        }
    }
}
