using Accounting.Application.Common.Exceptions;
using Accounting.Application.Common.Interfaces;
using Accounting.Application.Vouchers.Commands.Common;
using Accounting.Domain.Entity;
using Accounting.Domain.ValueObjects;

namespace Accounting.Application.Treasury.Commands.Common;

/// <summary>See <see cref="IReceiptVoucherBuilder"/> XML doc.</summary>
public sealed class ReceiptVoucherBuilder : IReceiptVoucherBuilder
{
    private const string ReceivablesAccountLabel = "حساب‌های دریافتنی";

    private readonly IVoucherHeadRepository _voucherHeadRepository;
    private readonly IVoucherDetailRepository _voucherDetailRepository;
    private readonly IVoucherTafsiliLevelGuard _tafsiliLevelGuard;
    private readonly ITreasurySettingReadRepository _settingReadRepository;
    private readonly IBankAccountReadRepository _bankAccountReadRepository;
    private readonly ITafsiliLookupReadRepository _tafsiliLookupReadRepository;
    private readonly ICurrentUser _currentUser;

    public ReceiptVoucherBuilder(
        IVoucherHeadRepository voucherHeadRepository,
        IVoucherDetailRepository voucherDetailRepository,
        IVoucherTafsiliLevelGuard tafsiliLevelGuard,
        ITreasurySettingReadRepository settingReadRepository,
        IBankAccountReadRepository bankAccountReadRepository,
        ITafsiliLookupReadRepository tafsiliLookupReadRepository,
        ICurrentUser currentUser)
    {
        _voucherHeadRepository = voucherHeadRepository;
        _voucherDetailRepository = voucherDetailRepository;
        _tafsiliLevelGuard = tafsiliLevelGuard;
        _settingReadRepository = settingReadRepository;
        _bankAccountReadRepository = bankAccountReadRepository;
        _tafsiliLookupReadRepository = tafsiliLookupReadRepository;
        _currentUser = currentUser;
    }

    public async Task<PaymentRequestVoucherBuildResult> BuildAndStageAsync(
        TB_TR_RECEIPT receipt, string vahedCode, CancellationToken cancellationToken = default)
    {
        var setting = await _settingReadRepository.GetByVahedAsync(vahedCode, cancellationToken);

        var receivablesAccountId = setting?.ReceivablesAccountId
            ?? throw new TreasurySettingValueMissingException(ReceivablesAccountLabel);

        var bankAccount = await _bankAccountReadRepository.GetByIdAsync(receipt.BANK_ACCOUNT_ID, vahedCode, cancellationToken)
            ?? throw new NotFoundException("BankAccount", receipt.BANK_ACCOUNT_ID);

        var bankAccountCodeId = bankAccount.AccountCodeId
            ?? throw new NotFoundException("BankAccountCode", receipt.BANK_ACCOUNT_ID);

        var bankLinks = bankAccount.TafsiliLinks
            .Select(l => new VoucherDetailTafsiliLinkInput(l.TafsiliId, l.LevelId))
            .ToList();

        var receivablesLinks = await ResolveReceivablesLinksAsync(receivablesAccountId, receipt.PAYER_TAFSILI_ID, cancellationToken);

        await EnsureTafsiliSatisfiedAsync(bankAccountCodeId, bankLinks, cancellationToken);
        await EnsureTafsiliSatisfiedAsync(receivablesAccountId, receivablesLinks, cancellationToken);

        var year = receipt.RECEIPT_DATE.Length >= 4 ? receipt.RECEIPT_DATE[..4] : receipt.RECEIPT_DATE;
        var docNum = await _voucherHeadRepository.GetNextDocNumAsync(vahedCode, year, cancellationToken);

        var now = DateTime.UtcNow;
        var userId = _currentUser.UserId;

        var head = new TB_VOUCHERSHEAD
        {
            ID = Guid.NewGuid(),
            DOC_NUM = docNum,
            DATE_DOC = receipt.RECEIPT_DATE,
            // owner decision ۲۰۲۶-۰۹-۲۹ — هر سند خودکار خزانه‌داری «موقت» است.
            DOCLIFE = DocLife.Temporary,
            HEAD_DESC = $"دریافت — {receipt.CODE} — {receipt.PAYER_NAME}",
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
            ACCOUNT_ID = bankAccountCodeId,
            DESCRIPTION = "دریافت — بانک",
            RADIF = 1,
            DEBTOR = receipt.AMOUNT,
            CREDITOR = 0,
            VAHEDCODE = vahedCode,
            YEAR = year,
            ADDUSERID = userId,
            CREATEDDATE = now,
            ISDELETED = false,
        };

        await _voucherDetailRepository.AddAsync(debitDetail, cancellationToken);
        await AddTafsiliLinksAsync(debitDetail.ID, bankLinks, vahedCode, year, now, userId, cancellationToken);
        lines.Add(new PaymentRequestVoucherLineResult(bankAccountCodeId, receipt.AMOUNT, 0, bankLinks));

        var creditDetail = new TB_VOUCHERSDETAIL
        {
            ID = Guid.NewGuid(),
            VOUCHERSHEAD_ID = head.ID,
            ACCOUNT_ID = receivablesAccountId,
            DESCRIPTION = "دریافت — حساب‌های دریافتنی",
            RADIF = 2,
            DEBTOR = 0,
            CREDITOR = receipt.AMOUNT,
            VAHEDCODE = vahedCode,
            YEAR = year,
            ADDUSERID = userId,
            CREATEDDATE = now,
            ISDELETED = false,
        };

        await _voucherDetailRepository.AddAsync(creditDetail, cancellationToken);
        await AddTafsiliLinksAsync(creditDetail.ID, receivablesLinks, vahedCode, year, now, userId, cancellationToken);
        lines.Add(new PaymentRequestVoucherLineResult(receivablesAccountId, 0, receipt.AMOUNT, receivablesLinks));

        // No balance assertion needed here — both lines are literally `receipt.AMOUNT`, so
        // debit == credit by construction, same posture as PaymentRequestPaymentVoucherBuilder.
        return new PaymentRequestVoucherBuildResult(head.ID, docNum, receipt.RECEIPT_DATE, year, lines);
    }

    /// <summary>
    /// «حساب‌های دریافتنی» — 0 configured levels ⇒ no link; exactly 1 ⇒ <c>PAYER_TAFSILI_ID</c> at
    /// that level (always present — the column is <c>NOT NULL</c>, so no "missing" case exists
    /// here unlike بخش-۴-ب's optional <c>BENEFICIARY_TAFSILI_ID</c>); more than 1 ⇒
    /// <see cref="TreasuryVoucherAccountConfigException"/> — same cardinality rules as بخش-۴-ب's
    /// <c>PaymentRequestPayablesTafsiliResolver</c> (owner instruction).
    /// </summary>
    private async Task<IReadOnlyList<VoucherDetailTafsiliLinkInput>> ResolveReceivablesLinksAsync(
        Guid receivablesAccountId, Guid payerTafsiliId, CancellationToken cancellationToken)
    {
        var levels = await _tafsiliLookupReadRepository.GetActiveLevelsAsync(receivablesAccountId, cancellationToken);

        if (levels.Count == 0)
        {
            return Array.Empty<VoucherDetailTafsiliLinkInput>();
        }

        if (levels.Count > 1)
        {
            throw new TreasuryVoucherAccountConfigException(
                ReceivablesAccountLabel, receivablesAccountId, "بیش از یک سطح تفصیلی الزامی است.");
        }

        return new[] { new VoucherDetailTafsiliLinkInput(payerTafsiliId, levels[0].LevelId) };
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
