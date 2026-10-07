using Accounting.Application.Common.Exceptions;
using Accounting.Application.Common.Interfaces;
using Accounting.Domain.Entity;
using Accounting.Domain.ValueObjects;

namespace Accounting.Application.Treasury.Commands.Common;

/// <summary>See <see cref="IBankStatementManualMatchService"/> XML doc.</summary>
public sealed class BankStatementManualMatchService : IBankStatementManualMatchService
{
    private readonly IVoucherDetailRepository _voucherDetailRepository;
    private readonly IBankAccountReadRepository _bankAccountReadRepository;
    private readonly ITreasuryBankStatementLineRepository _lineRepository;
    private readonly ICurrentUser _currentUser;
    private readonly IBankCardRepository? _bankCards;

    public BankStatementManualMatchService(
        IVoucherDetailRepository voucherDetailRepository,
        IBankAccountReadRepository bankAccountReadRepository,
        ITreasuryBankStatementLineRepository lineRepository,
        ICurrentUser currentUser,
        IBankCardRepository? bankCards = null)
    {
        _voucherDetailRepository = voucherDetailRepository;
        _bankAccountReadRepository = bankAccountReadRepository;
        _lineRepository = lineRepository;
        _currentUser = currentUser;
        _bankCards = bankCards;
    }

    public async Task MatchAsync(
        TB_TR_BANK_STATEMENT statement,
        TB_TR_BANK_STATEMENT_LINE line,
        Guid voucherDetailId,
        string vahedCode,
        CancellationToken cancellationToken = default)
    {
        if (line.MATCH_STATE != BankStatementLineMatchState.Unmatched)
        {
            throw new TreasuryBankStatementLineStateConflictException(line.ID, line.MATCH_STATE, "نامنطبق");
        }

        var detail = await _voucherDetailRepository.GetForUpdateAsync(voucherDetailId, vahedCode, cancellationToken)
            ?? throw new NotFoundException("VoucherDetail", voucherDetailId);

        var bankAccount = await _bankAccountReadRepository.GetByIdAsync(statement.BANK_ACCOUNT_ID, vahedCode, cancellationToken)
            ?? throw new NotFoundException("BankAccount", statement.BANK_ACCOUNT_ID);

        if (detail.ACCOUNT_ID != bankAccount.AccountCodeId)
        {
            throw new TreasuryBankStatementMatchMismatchException("ردیف دفتری انتخاب‌شده به معین این حساب بانکی تعلق ندارد.");
        }

        var isDeposit = line.DEPOSIT > 0;
        var lineAmount = isDeposit ? line.DEPOSIT : line.WITHDRAWAL;
        var detailAmount = isDeposit ? detail.DEBTOR ?? 0m : detail.CREDITOR ?? 0m;

        if (detailAmount <= 0)
        {
            throw new TreasuryBankStatementMatchMismatchException(
                isDeposit ? "ردیف دفتری انتخاب‌شده بدهکار نیست." : "ردیف دفتری انتخاب‌شده بستانکار نیست.");
        }

        if (detailAmount != lineAmount)
        {
            throw new TreasuryBankStatementMatchMismatchException("مبلغ ردیف دفتری با مبلغ ردیف صورت‌حساب برابر نیست.");
        }

        var claimed = await _lineRepository.GetMatchedVoucherDetailIdsAsync(vahedCode, cancellationToken);

        if (claimed.Contains(voucherDetailId))
        {
            throw new TreasuryBankStatementMatchMismatchException("این ردیف دفتری قبلاً به ردیف دیگری تطبیق داده شده است.");
        }

        line.MATCH_STATE = BankStatementLineMatchState.ManualMatched;
        line.MATCHED_VOUCHERDETAIL_ID = voucherDetailId;
        line.CHANGEUSERID = _currentUser.UserId;
        line.UPDATEDDATE = DateTime.UtcNow;

        await BankStatementReceivedDate.SetAsync(
            _voucherDetailRepository, _bankCards, voucherDetailId, vahedCode, line.LINE_DATE, cancellationToken);
    }

    public async Task UnmatchAsync(TB_TR_BANK_STATEMENT_LINE line, CancellationToken cancellationToken = default)
    {
        if (line.MATCH_STATE is not (BankStatementLineMatchState.AutoMatched or BankStatementLineMatchState.ManualMatched))
        {
            throw new TreasuryBankStatementLineStateConflictException(line.ID, line.MATCH_STATE, "تطبیق‌خودکار یا تطبیق‌دستی");
        }

        var previous = line.MATCHED_VOUCHERDETAIL_ID;

        line.MATCH_STATE = BankStatementLineMatchState.Unmatched;
        line.MATCHED_VOUCHERDETAIL_ID = null;
        line.CHANGEUSERID = _currentUser.UserId;
        line.UPDATEDDATE = DateTime.UtcNow;

        if (previous is { } detailId)
        {
            await BankStatementReceivedDate.SetAsync(
                _voucherDetailRepository, _bankCards, detailId, line.VAHEDCODE, null, cancellationToken);
        }
    }
}
