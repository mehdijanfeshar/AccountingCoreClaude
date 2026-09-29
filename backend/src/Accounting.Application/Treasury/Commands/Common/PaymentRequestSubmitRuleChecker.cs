using System.Globalization;
using Accounting.Application.Common.Exceptions;
using Accounting.Application.Common.Interfaces;

namespace Accounting.Application.Treasury.Commands.Common;

public sealed class PaymentRequestSubmitRuleChecker : IPaymentRequestSubmitRuleChecker
{
    private readonly ITreasurySettingReadRepository _settingReadRepository;
    private readonly IPaymentRequestRepository _paymentRequestRepository;

    public PaymentRequestSubmitRuleChecker(
        ITreasurySettingReadRepository settingReadRepository,
        IPaymentRequestRepository paymentRequestRepository)
    {
        _settingReadRepository = settingReadRepository;
        _paymentRequestRepository = paymentRequestRepository;
    }

    public async Task EnsureSubmittableAsync(
        Guid paymentRequestId,
        string? beneficiaryNationalId,
        string? invoiceRef,
        bool invoiceApproved,
        string dueDate,
        string vahedCode,
        Guid? excludeId,
        CancellationToken cancellationToken = default)
    {
        var setting = await _settingReadRepository.GetByVahedAsync(vahedCode, cancellationToken);

        if (setting is null)
        {
            throw new PaymentRequestSettingsMissingException(vahedCode);
        }

        if (string.CompareOrdinal(dueDate, TodayJalali()) <= 0)
        {
            throw new PaymentRequestDueDatePastException(paymentRequestId, dueDate);
        }

        if (!string.IsNullOrWhiteSpace(invoiceRef) && !invoiceApproved)
        {
            throw new PaymentRequestInvoiceNotApprovedException(paymentRequestId);
        }

        if (!string.IsNullOrWhiteSpace(beneficiaryNationalId) && !string.IsNullOrWhiteSpace(invoiceRef))
        {
            var duplicate = await _paymentRequestRepository.ExistsDuplicateAsync(
                beneficiaryNationalId, invoiceRef, vahedCode, excludeId, cancellationToken);

            if (duplicate)
            {
                throw new PaymentRequestDuplicateException(paymentRequestId, beneficiaryNationalId, invoiceRef);
            }
        }
    }

    /// <summary>Today as a Legacy <c>YYYYMMDD</c> Jalali string — same approach as
    /// <c>ReverseVoucherCommandHandler.TodayJalali</c>/<c>ReturnPettyCashExpenseDocCommandValidator.TodayJalali</c>.</summary>
    private static string TodayJalali()
    {
        var calendar = new PersianCalendar();
        var now = DateTime.Now;

        return $"{calendar.GetYear(now):0000}{calendar.GetMonth(now):00}{calendar.GetDayOfMonth(now):00}";
    }
}
