using Accounting.Application.Common.Exceptions;
using Accounting.Application.Common.Interfaces;
using Accounting.Domain.Entity;
using Accounting.Domain.ValueObjects;

namespace Accounting.Application.Treasury.Commands.Common;

/// <summary>See <see cref="ITransferApprovalService"/> XML doc.</summary>
public sealed class TransferApprovalService : ITransferApprovalService
{
    private const string DailyTransferLimitLabel = "سقف روزانهٔ انتقال";

    private readonly ITreasuryTransferRepository _transferRepository;
    private readonly ITreasuryTransferEventRepository _eventRepository;
    private readonly ITreasuryTreasurerAuthorizer _treasurerAuthorizer;
    private readonly ITreasurySettingReadRepository _settingReadRepository;
    private readonly IBankAccountReadRepository _bankAccountReadRepository;
    private readonly ITreasuryBankAccountBalanceReadRepository _balanceReadRepository;
    private readonly ITransferVoucherBuilder _voucherBuilder;
    private readonly ICurrentUser _currentUser;
    private readonly IClientInfoProvider _clientInfoProvider;

    public TransferApprovalService(
        ITreasuryTransferRepository transferRepository,
        ITreasuryTransferEventRepository eventRepository,
        ITreasuryTreasurerAuthorizer treasurerAuthorizer,
        ITreasurySettingReadRepository settingReadRepository,
        IBankAccountReadRepository bankAccountReadRepository,
        ITreasuryBankAccountBalanceReadRepository balanceReadRepository,
        ITransferVoucherBuilder voucherBuilder,
        ICurrentUser currentUser,
        IClientInfoProvider clientInfoProvider)
    {
        _transferRepository = transferRepository;
        _eventRepository = eventRepository;
        _treasurerAuthorizer = treasurerAuthorizer;
        _settingReadRepository = settingReadRepository;
        _bankAccountReadRepository = bankAccountReadRepository;
        _balanceReadRepository = balanceReadRepository;
        _voucherBuilder = voucherBuilder;
        _currentUser = currentUser;
        _clientInfoProvider = clientInfoProvider;
    }

    public async Task<TB_TR_TRANSFER> ApproveAsync(
        Guid id, string vahedCode, string bankReference, CancellationToken cancellationToken = default)
    {
        var transfer = await LoadPendingAsync(id, vahedCode, cancellationToken);

        await _treasurerAuthorizer.EnsureTreasurerAsync(vahedCode, cancellationToken);

        EnsureNotCreator(transfer, id);

        var sourceBankAccount = await _bankAccountReadRepository.GetByIdAsync(transfer.SOURCE_BANK_ACCOUNT_ID, vahedCode, cancellationToken)
            ?? throw new NotFoundException("BankAccount", transfer.SOURCE_BANK_ACCOUNT_ID);

        var sourceAccountCodeId = sourceBankAccount.AccountCodeId
            ?? throw new NotFoundException("BankAccountCode", transfer.SOURCE_BANK_ACCOUNT_ID);

        var year = transfer.TRANSFER_DATE.Length >= 4 ? transfer.TRANSFER_DATE[..4] : transfer.TRANSFER_DATE;

        // Blocking control (a) — owner decision ۲۰۲۶-۰۹-۲۹: موجودی حساب معین بانک مبدأ روی تمام
        // اسناد غیرحذف‌شدهٔ سال (موقت هم شامل)، محدود به تفصیلی(های) همین حساب بانکی.
        var sourceTafsiliIds = sourceBankAccount.TafsiliLinks.Select(l => l.TafsiliId).ToList();
        var currentBalance = await _balanceReadRepository.GetBalanceAsync(
            sourceAccountCodeId, sourceTafsiliIds, vahedCode, year, cancellationToken: cancellationToken);

        if (currentBalance < transfer.AMOUNT)
        {
            throw new TreasuryTransferInsufficientBalanceException(id, currentBalance, transfer.AMOUNT);
        }

        // Blocking control (b) — سقف روزانهٔ انتقال از همین مبدأ، همین تاریخ.
        var setting = await _settingReadRepository.GetByVahedAsync(vahedCode, cancellationToken);

        var dailyLimit = setting?.DailyTransferLimit
            ?? throw new TreasurySettingValueMissingException(DailyTransferLimitLabel);

        var alreadyTransferredToday = await _transferRepository.GetExecutedAmountForSourceOnDateAsync(
            transfer.SOURCE_BANK_ACCOUNT_ID, transfer.TRANSFER_DATE, excludeId: id, cancellationToken);

        if (alreadyTransferredToday + transfer.AMOUNT > dailyLimit)
        {
            throw new TreasuryTransferDailyLimitExceededException(id, alreadyTransferredToday, transfer.AMOUNT, dailyLimit);
        }

        var voucherResult = await _voucherBuilder.BuildAndStageAsync(transfer, vahedCode, cancellationToken);

        var now = DateTime.UtcNow;
        var userId = _currentUser.UserId;

        transfer.VOUCHER_ID = voucherResult.VoucherHeadId;
        transfer.BANK_REFERENCE = bankReference;
        transfer.APPROVED_BY = userId;
        transfer.APPROVED_DATE = now;
        transfer.STATE = TransferState.Executed;
        transfer.CHANGEUSERID = userId;
        transfer.UPDATEDDATE = now;

        await AddEventAsync(
            transfer,
            TransferEventAction.Approve,
            TransferState.PendingTreasurer,
            TransferState.Executed,
            $"سند انتقال شمارهٔ {voucherResult.DocNum} صادر شد — مرجع بانکی {bankReference}.",
            vahedCode,
            cancellationToken);

        return transfer;
    }

    public async Task ReturnAsync(Guid id, string vahedCode, string reason, CancellationToken cancellationToken = default)
    {
        var transfer = await LoadPendingAsync(id, vahedCode, cancellationToken);

        await _treasurerAuthorizer.EnsureTreasurerAsync(vahedCode, cancellationToken);

        EnsureNotCreator(transfer, id);

        transfer.STATE = TransferState.Returned;
        transfer.RETURN_REASON = reason;
        transfer.CHANGEUSERID = _currentUser.UserId;
        transfer.UPDATEDDATE = DateTime.UtcNow;

        await AddEventAsync(transfer, TransferEventAction.Return, TransferState.PendingTreasurer, TransferState.Returned, reason, vahedCode, cancellationToken);
    }

    public async Task RejectAsync(Guid id, string vahedCode, string reason, CancellationToken cancellationToken = default)
    {
        var transfer = await LoadPendingAsync(id, vahedCode, cancellationToken);

        await _treasurerAuthorizer.EnsureTreasurerAsync(vahedCode, cancellationToken);

        EnsureNotCreator(transfer, id);

        transfer.STATE = TransferState.Rejected;
        transfer.CHANGEUSERID = _currentUser.UserId;
        transfer.UPDATEDDATE = DateTime.UtcNow;

        await AddEventAsync(transfer, TransferEventAction.Reject, TransferState.PendingTreasurer, TransferState.Rejected, reason, vahedCode, cancellationToken);
    }

    private async Task<TB_TR_TRANSFER> LoadPendingAsync(Guid id, string vahedCode, CancellationToken cancellationToken)
    {
        var transfer = await _transferRepository.GetForUpdateAsync(id, vahedCode, cancellationToken);

        if (transfer is null || transfer.ISDELETED)
        {
            throw new NotFoundException("Transfer", id);
        }

        if (transfer.STATE != TransferState.PendingTreasurer)
        {
            throw new TreasuryTransferStateConflictException(id, transfer.STATE, "در انتظار اقدام خزانه‌دار");
        }

        return transfer;
    }

    /// <summary>SoD: «تأییدکننده/برگشت‌دهنده/ردکننده ≠ ثبت‌کننده» — applied uniformly across
    /// approve/return/reject, same posture as <c>PaymentRequestApprovalService.EnsureNotCreator</c>.</summary>
    private void EnsureNotCreator(TB_TR_TRANSFER transfer, Guid id)
    {
        if (string.Equals(transfer.ADDUSERID, _currentUser.UserId, StringComparison.Ordinal))
        {
            throw new TreasuryTransferApproverConflictException(id);
        }
    }

    private async Task AddEventAsync(
        TB_TR_TRANSFER transfer,
        TransferEventAction action,
        TransferState fromState,
        TransferState toState,
        string? note,
        string vahedCode,
        CancellationToken cancellationToken)
    {
        await _eventRepository.AddAsync(
            new TB_TR_TRANSFER_EVENT
            {
                ID = Guid.NewGuid(),
                TRANSFER_ID = transfer.ID,
                ACTION = action,
                FROM_STATE = fromState,
                TO_STATE = toState,
                NOTE = note,
                CLIENT_IP = _clientInfoProvider.ClientIp,
                VAHEDCODE = vahedCode,
                YEAR = transfer.YEAR,
                ADDUSERID = _currentUser.UserId,
                CREATEDDATE = DateTime.UtcNow,
                ISDELETED = false,
            },
            cancellationToken);
    }
}
