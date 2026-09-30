using Accounting.Application.Common.Exceptions;
using Accounting.Application.Common.Interfaces;
using Accounting.Domain.Entity;
using Accounting.Domain.ValueObjects;

namespace Accounting.Application.Treasury.Commands.Common;

/// <summary>See <see cref="IBankStatementLineResolutionService"/> XML doc.</summary>
public sealed class BankStatementLineResolutionService : IBankStatementLineResolutionService
{
    private readonly IBankFeeVoucherBuilder _bankFeeVoucherBuilder;
    private readonly ITreasuryReceiptRepository _receiptRepository;
    private readonly IVoucherHeadRepository _voucherHeadRepository;
    private readonly ICurrentUser _currentUser;

    public BankStatementLineResolutionService(
        IBankFeeVoucherBuilder bankFeeVoucherBuilder,
        ITreasuryReceiptRepository receiptRepository,
        IVoucherHeadRepository voucherHeadRepository,
        ICurrentUser currentUser)
    {
        _bankFeeVoucherBuilder = bankFeeVoucherBuilder;
        _receiptRepository = receiptRepository;
        _voucherHeadRepository = voucherHeadRepository;
        _currentUser = currentUser;
    }

    public async Task ResolveAsync(
        TB_TR_BANK_STATEMENT statement,
        TB_TR_BANK_STATEMENT_LINE line,
        BankStatementLineResolutionType type,
        Guid? receiptId,
        string? note,
        string vahedCode,
        CancellationToken cancellationToken = default)
    {
        if (line.MATCH_STATE != BankStatementLineMatchState.Unmatched)
        {
            throw new TreasuryBankStatementLineStateConflictException(line.ID, line.MATCH_STATE, "نامنطبق");
        }

        var now = DateTime.UtcNow;
        var userId = _currentUser.UserId;

        switch (type)
        {
            case BankStatementLineResolutionType.BankFeeVoucher:
            {
                if (line.WITHDRAWAL <= 0)
                {
                    throw new TreasuryBankStatementResolutionInvalidException(
                        "«سند کارمزد بانکی» فقط روی ردیف‌های برداشتی ممکن است.");
                }

                var voucherResult = await _bankFeeVoucherBuilder.BuildAndStageAsync(
                    line, statement.BANK_ACCOUNT_ID, vahedCode, cancellationToken);

                line.RESOLUTION_VOUCHER_ID = voucherResult.VoucherHeadId;
                break;
            }

            case BankStatementLineResolutionType.LinkedReceipt:
            {
                if (line.DEPOSIT <= 0)
                {
                    throw new TreasuryBankStatementResolutionInvalidException(
                        "«دریافت مرتبط» فقط روی ردیف‌های واریزی ممکن است.");
                }

                if (receiptId is not { } id)
                {
                    throw new TreasuryBankStatementResolutionInvalidException("شناسهٔ دریافت وجه الزامی است.");
                }

                var receipt = await _receiptRepository.GetForUpdateAsync(id, vahedCode, cancellationToken)
                    ?? throw new NotFoundException("Receipt", id);

                if (receipt.STATE != ReceiptState.Registered)
                {
                    throw new TreasuryBankStatementResolutionInvalidException("دریافت وجه انتخاب‌شده ثبت‌شده نیست.");
                }

                if (receipt.BANK_ACCOUNT_ID != statement.BANK_ACCOUNT_ID)
                {
                    throw new TreasuryBankStatementResolutionInvalidException("دریافت وجه انتخاب‌شده متعلق به حساب بانکی دیگری است.");
                }

                if (receipt.AMOUNT != line.DEPOSIT)
                {
                    throw new TreasuryBankStatementResolutionInvalidException("مبلغ دریافت وجه انتخاب‌شده با مبلغ ردیف صورت‌حساب برابر نیست.");
                }

                line.RESOLUTION_RECEIPT_ID = id;
                break;
            }

            case BankStatementLineResolutionType.Ignored:
            {
                if (string.IsNullOrWhiteSpace(note))
                {
                    throw new TreasuryBankStatementResolutionInvalidException("برای «نادیده‌گرفته‌شده» ثبت یادداشت الزامی است.");
                }

                break;
            }

            default:
                throw new TreasuryBankStatementResolutionInvalidException("نوع حل نامعتبر است.");
        }

        line.RESOLUTION_TYPE = type;
        line.RESOLUTION_NOTE = note;
        line.MATCH_STATE = BankStatementLineMatchState.Resolved;
        line.CHANGEUSERID = userId;
        line.UPDATEDDATE = now;
    }

    public async Task UnresolveAsync(TB_TR_BANK_STATEMENT_LINE line, string vahedCode, CancellationToken cancellationToken = default)
    {
        if (line.MATCH_STATE != BankStatementLineMatchState.Resolved)
        {
            throw new TreasuryBankStatementLineStateConflictException(line.ID, line.MATCH_STATE, "حل‌شده");
        }

        if (line.RESOLUTION_TYPE == BankStatementLineResolutionType.BankFeeVoucher && line.RESOLUTION_VOUCHER_ID is { } voucherId)
        {
            var voucher = await _voucherHeadRepository.GetForUpdateAsync(voucherId, vahedCode, cancellationToken);

            if (voucher is null || voucher.ISDELETED == true)
            {
                // سند از قبل حذف شده — چیزی برای حذف دوباره نیست، ادامه به پاک‌کردن فیلدهای حل.
            }
            else if (voucher.DOCLIFE != DocLife.Temporary)
            {
                throw new TreasuryBankStatementUnresolveNotAllowedException(line.ID);
            }
            else
            {
                var now = DateTime.UtcNow;
                var userId = _currentUser.UserId;

                voucher.ISDELETED = true;
                voucher.CHANGEUSERID = userId;
                voucher.UPDATEDDATE = now;

                await _voucherHeadRepository.SoftDeleteDetailTreeAsync(voucher.ID, userId, now, cancellationToken);
            }
        }

        line.MATCH_STATE = BankStatementLineMatchState.Unmatched;
        line.RESOLUTION_TYPE = null;
        line.RESOLUTION_VOUCHER_ID = null;
        line.RESOLUTION_RECEIPT_ID = null;
        line.RESOLUTION_NOTE = null;
        line.CHANGEUSERID = _currentUser.UserId;
        line.UPDATEDDATE = DateTime.UtcNow;
    }
}
