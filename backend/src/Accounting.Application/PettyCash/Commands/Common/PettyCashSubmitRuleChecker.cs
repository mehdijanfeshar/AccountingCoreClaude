using Accounting.Application.Common.Exceptions;
using Accounting.Application.Common.Interfaces;

namespace Accounting.Application.PettyCash.Commands.Common;

public sealed class PettyCashSubmitRuleChecker : IPettyCashSubmitRuleChecker
{
    private readonly IPettyCashFundSettingRepository _fundSettingRepository;
    private readonly IPettyCashExpenseDocRepository _expenseDocRepository;

    public PettyCashSubmitRuleChecker(
        IPettyCashFundSettingRepository fundSettingRepository,
        IPettyCashExpenseDocRepository expenseDocRepository)
    {
        _fundSettingRepository = fundSettingRepository;
        _expenseDocRepository = expenseDocRepository;
    }

    public async Task EnsureSubmittableAsync(
        Guid expenseDocId,
        Guid revolvingFundId,
        decimal? fundCeiling,
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

        var settings = await _fundSettingRepository.GetByFundIdAsync(revolvingFundId, cancellationToken);

        if (settings is { PER_DOC_LIMIT: { } limit } && totalAmount > limit)
        {
            throw new PettyCashPerDocLimitExceededException(expenseDocId, totalAmount, limit);
        }

        var exposure = await _expenseDocRepository.GetFundExposureAsync(
            revolvingFundId, vahedCode, excludeDocId, cancellationToken);

        var cashBalance = (fundCeiling ?? 0m) - exposure.ApprovedAmount - exposure.InFlightAmount;

        if (totalAmount > cashBalance)
        {
            throw new PettyCashInsufficientCashBalanceException(expenseDocId, totalAmount, cashBalance);
        }
    }
}
