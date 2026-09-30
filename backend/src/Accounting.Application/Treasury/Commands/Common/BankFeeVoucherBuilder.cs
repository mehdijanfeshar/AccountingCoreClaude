using Accounting.Application.Common.Exceptions;
using Accounting.Application.Common.Interfaces;
using Accounting.Application.Vouchers.Commands.Common;
using Accounting.Domain.Entity;
using Accounting.Domain.ValueObjects;

namespace Accounting.Application.Treasury.Commands.Common;

/// <summary>See <see cref="IBankFeeVoucherBuilder"/> XML doc.</summary>
public sealed class BankFeeVoucherBuilder : IBankFeeVoucherBuilder
{
    private const string BankFeeAccountLabel = "کارمزد بانکی";

    private readonly IVoucherHeadRepository _voucherHeadRepository;
    private readonly IVoucherDetailRepository _voucherDetailRepository;
    private readonly IVoucherTafsiliLevelGuard _tafsiliLevelGuard;
    private readonly ITreasurySettingReadRepository _settingReadRepository;
    private readonly IBankAccountReadRepository _bankAccountReadRepository;
    private readonly ITafsiliLookupReadRepository _tafsiliLookupReadRepository;
    private readonly ICurrentUser _currentUser;

    public BankFeeVoucherBuilder(
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
        TB_TR_BANK_STATEMENT_LINE line, Guid bankAccountId, string vahedCode, CancellationToken cancellationToken = default)
    {
        var setting = await _settingReadRepository.GetByVahedAsync(vahedCode, cancellationToken);

        var feeAccountId = setting?.BankFeeAccountId
            ?? throw new TreasurySettingValueMissingException(BankFeeAccountLabel);

        // حساب کارمزد ورودی تفصیلی نمی‌گیرد — اگر پیکربندی سطح الزامی داشته باشد، خطای پیکربندی
        // (همان قاعدهٔ IPaymentRequestPayablesTafsiliResolver.EnsureNoTafsiliRequiredAsync، اینجا
        // مستقیم تکرار شده چون آن resolver مخصوص استثناهای بخش ۴-ب است).
        var feeAccountLevels = await _tafsiliLookupReadRepository.GetActiveLevelsAsync(feeAccountId, cancellationToken);

        if (feeAccountLevels.Count > 0)
        {
            throw new TreasuryVoucherAccountConfigException(
                BankFeeAccountLabel, feeAccountId, "این حساب نباید سطح تفصیلی الزامی داشته باشد.");
        }

        var bankAccount = await _bankAccountReadRepository.GetByIdAsync(bankAccountId, vahedCode, cancellationToken)
            ?? throw new NotFoundException("BankAccount", bankAccountId);

        var bankAccountCodeId = bankAccount.AccountCodeId
            ?? throw new NotFoundException("BankAccountCode", bankAccountId);

        var bankLinks = bankAccount.TafsiliLinks
            .Select(l => new VoucherDetailTafsiliLinkInput(l.TafsiliId, l.LevelId))
            .ToList();

        await EnsureTafsiliSatisfiedAsync(bankAccountCodeId, bankLinks, cancellationToken);

        var year = line.LINE_DATE.Length >= 4 ? line.LINE_DATE[..4] : line.LINE_DATE;
        var docNum = await _voucherHeadRepository.GetNextDocNumAsync(vahedCode, year, cancellationToken);

        var now = DateTime.UtcNow;
        var userId = _currentUser.UserId;

        var head = new TB_VOUCHERSHEAD
        {
            ID = Guid.NewGuid(),
            DOC_NUM = docNum,
            DATE_DOC = line.LINE_DATE,
            DOCLIFE = DocLife.Temporary,
            HEAD_DESC = $"کارمزد بانکی — مغایرت‌گیری {line.DESCRIPTION}".Trim(),
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
            ACCOUNT_ID = feeAccountId,
            DESCRIPTION = "کارمزد بانکی",
            RADIF = 1,
            DEBTOR = line.WITHDRAWAL,
            CREDITOR = 0,
            VAHEDCODE = vahedCode,
            YEAR = year,
            ADDUSERID = userId,
            CREATEDDATE = now,
            ISDELETED = false,
        };

        await _voucherDetailRepository.AddAsync(debitDetail, cancellationToken);
        lines.Add(new PaymentRequestVoucherLineResult(feeAccountId, line.WITHDRAWAL, 0, Array.Empty<VoucherDetailTafsiliLinkInput>()));

        var creditDetail = new TB_VOUCHERSDETAIL
        {
            ID = Guid.NewGuid(),
            VOUCHERSHEAD_ID = head.ID,
            ACCOUNT_ID = bankAccountCodeId,
            DESCRIPTION = "کارمزد بانکی — بانک",
            RADIF = 2,
            DEBTOR = 0,
            CREDITOR = line.WITHDRAWAL,
            VAHEDCODE = vahedCode,
            YEAR = year,
            ADDUSERID = userId,
            CREATEDDATE = now,
            ISDELETED = false,
        };

        await _voucherDetailRepository.AddAsync(creditDetail, cancellationToken);
        await AddTafsiliLinksAsync(creditDetail.ID, bankLinks, vahedCode, year, now, userId, cancellationToken);
        lines.Add(new PaymentRequestVoucherLineResult(bankAccountCodeId, 0, line.WITHDRAWAL, bankLinks));

        // بدون Assertion تراز — هر دو ردیف عیناً line.WITHDRAWAL‌اند، پس برابری بدهکار/بستانکار
        // خودبه‌خود برقرار است (همان الگوی TransferVoucherBuilder/ReceiptVoucherBuilder).
        return new PaymentRequestVoucherBuildResult(head.ID, docNum, line.LINE_DATE, year, lines);
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
