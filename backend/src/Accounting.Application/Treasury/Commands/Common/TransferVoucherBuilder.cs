using Accounting.Application.Common.Exceptions;
using Accounting.Application.Common.Interfaces;
using Accounting.Application.Vouchers.Commands.Common;
using Accounting.Domain.Entity;
using Accounting.Domain.ValueObjects;

namespace Accounting.Application.Treasury.Commands.Common;

/// <summary>See <see cref="ITransferVoucherBuilder"/> XML doc.</summary>
public sealed class TransferVoucherBuilder : ITransferVoucherBuilder
{
    private readonly IVoucherHeadRepository _voucherHeadRepository;
    private readonly IVoucherDetailRepository _voucherDetailRepository;
    private readonly IVoucherTafsiliLevelGuard _tafsiliLevelGuard;
    private readonly IBankAccountReadRepository _bankAccountReadRepository;
    private readonly ICurrentUser _currentUser;

    public TransferVoucherBuilder(
        IVoucherHeadRepository voucherHeadRepository,
        IVoucherDetailRepository voucherDetailRepository,
        IVoucherTafsiliLevelGuard tafsiliLevelGuard,
        IBankAccountReadRepository bankAccountReadRepository,
        ICurrentUser currentUser)
    {
        _voucherHeadRepository = voucherHeadRepository;
        _voucherDetailRepository = voucherDetailRepository;
        _tafsiliLevelGuard = tafsiliLevelGuard;
        _bankAccountReadRepository = bankAccountReadRepository;
        _currentUser = currentUser;
    }

    public async Task<PaymentRequestVoucherBuildResult> BuildAndStageAsync(
        TB_TR_TRANSFER transfer, string vahedCode, CancellationToken cancellationToken = default)
    {
        var (sourceAccountCodeId, sourceLinks) = await ResolveBankAsync(transfer.SOURCE_BANK_ACCOUNT_ID, vahedCode, cancellationToken);
        var (destAccountCodeId, destLinks) = await ResolveBankAsync(transfer.DEST_BANK_ACCOUNT_ID, vahedCode, cancellationToken);

        await EnsureTafsiliSatisfiedAsync(sourceAccountCodeId, sourceLinks, cancellationToken);
        await EnsureTafsiliSatisfiedAsync(destAccountCodeId, destLinks, cancellationToken);

        var year = transfer.TRANSFER_DATE.Length >= 4 ? transfer.TRANSFER_DATE[..4] : transfer.TRANSFER_DATE;
        var docNum = await _voucherHeadRepository.GetNextDocNumAsync(vahedCode, year, cancellationToken);

        var now = DateTime.UtcNow;
        var userId = _currentUser.UserId;

        var head = new TB_VOUCHERSHEAD
        {
            ID = Guid.NewGuid(),
            DOC_NUM = docNum,
            DATE_DOC = transfer.TRANSFER_DATE,
            DOCLIFE = DocLife.Temporary,
            HEAD_DESC = $"انتقال — {transfer.CODE}",
            VAHEDCODE = vahedCode,
            YEAR = year,
            ISAUTOMATIC = true,
            ADDUSERID = userId,
            CREATEDDATE = now,
            ISDELETED = false,
        };

        await _voucherHeadRepository.AddAsync(head, cancellationToken);

        var lines = new List<PaymentRequestVoucherLineResult>();

        var debitDetail = new TB_VOUCHERSDETAIL
        {
            ID = Guid.NewGuid(),
            VOUCHERSHEAD_ID = head.ID,
            ACCOUNT_ID = destAccountCodeId,
            DESCRIPTION = "انتقال — بانک مقصد",
            RADIF = 1,
            DEBTOR = transfer.AMOUNT,
            CREDITOR = 0,
            VAHEDCODE = vahedCode,
            YEAR = year,
            ADDUSERID = userId,
            CREATEDDATE = now,
            ISDELETED = false,
        };

        await _voucherDetailRepository.AddAsync(debitDetail, cancellationToken);
        await AddTafsiliLinksAsync(debitDetail.ID, destLinks, vahedCode, year, now, userId, cancellationToken);
        lines.Add(new PaymentRequestVoucherLineResult(destAccountCodeId, transfer.AMOUNT, 0, destLinks));

        var creditDetail = new TB_VOUCHERSDETAIL
        {
            ID = Guid.NewGuid(),
            VOUCHERSHEAD_ID = head.ID,
            ACCOUNT_ID = sourceAccountCodeId,
            DESCRIPTION = "انتقال — بانک مبدأ",
            RADIF = 2,
            DEBTOR = 0,
            CREDITOR = transfer.AMOUNT,
            VAHEDCODE = vahedCode,
            YEAR = year,
            ADDUSERID = userId,
            CREATEDDATE = now,
            ISDELETED = false,
        };

        await _voucherDetailRepository.AddAsync(creditDetail, cancellationToken);
        await AddTafsiliLinksAsync(creditDetail.ID, sourceLinks, vahedCode, year, now, userId, cancellationToken);
        lines.Add(new PaymentRequestVoucherLineResult(sourceAccountCodeId, 0, transfer.AMOUNT, sourceLinks));

        // No balance assertion needed — both lines are literally `transfer.AMOUNT`.
        return new PaymentRequestVoucherBuildResult(head.ID, docNum, transfer.TRANSFER_DATE, year, lines);
    }

    private async Task<(Guid AccountCodeId, IReadOnlyList<VoucherDetailTafsiliLinkInput> Links)> ResolveBankAsync(
        Guid bankAccountId, string vahedCode, CancellationToken cancellationToken)
    {
        var bankAccount = await _bankAccountReadRepository.GetByIdAsync(bankAccountId, vahedCode, cancellationToken)
            ?? throw new NotFoundException("BankAccount", bankAccountId);

        var accountCodeId = bankAccount.AccountCodeId
            ?? throw new NotFoundException("BankAccountCode", bankAccountId);

        var links = bankAccount.TafsiliLinks
            .Select(l => new VoucherDetailTafsiliLinkInput(l.TafsiliId, l.LevelId))
            .ToList();

        return (accountCodeId, links);
    }

    private async Task AddTafsiliLinksAsync(
        Guid voucherDetailId,
        IReadOnlyList<VoucherDetailTafsiliLinkInput> tafsiliLinks,
        string vahedCode,
        string year,
        DateTime now,
        string userId,
        CancellationToken cancellationToken)
    {
        foreach (var tafsili in tafsiliLinks)
        {
            await _voucherDetailRepository.AddTafsiliLinkAsync(
                new TB_VOUCHERDETAIL_LINK_TAFSILI
                {
                    ID = Guid.NewGuid(),
                    VOUCHERSDETAIL_ID = voucherDetailId,
                    TAFSILI_ID = tafsili.TafsiliId,
                    LEVEL_ID = tafsili.LevelId,
                    VAHEDCODE = vahedCode,
                    YEAR = year,
                    ADDUSERID = userId,
                    CREATEDDATE = now,
                    ISDELETED = false,
                },
                cancellationToken);
        }
    }

    private async Task EnsureTafsiliSatisfiedAsync(
        Guid accountCodeId, IReadOnlyList<VoucherDetailTafsiliLinkInput> tafsiliLinks, CancellationToken cancellationToken)
    {
        try
        {
            await _tafsiliLevelGuard.EnsureSatisfiedAsync(accountCodeId, tafsiliLinks, cancellationToken);
        }
        catch (TafsiliLevelRuleException ex)
        {
            throw new TreasuryVoucherTafsiliMissingException(accountCodeId, ex.PublicDetail);
        }
    }
}
