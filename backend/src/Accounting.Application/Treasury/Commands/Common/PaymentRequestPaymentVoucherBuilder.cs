using System.Globalization;
using Accounting.Application.Common.Exceptions;
using Accounting.Application.Common.Interfaces;
using Accounting.Application.Vouchers.Commands.Common;
using Accounting.Domain.Entity;
using Accounting.Domain.ValueObjects;

namespace Accounting.Application.Treasury.Commands.Common;

/// <summary>See <see cref="IPaymentRequestPaymentVoucherBuilder"/> XML doc.</summary>
public sealed class PaymentRequestPaymentVoucherBuilder : IPaymentRequestPaymentVoucherBuilder
{
    private const string PayablesAccountLabel = "حساب بستانکاران";

    private readonly IVoucherHeadRepository _voucherHeadRepository;
    private readonly IVoucherDetailRepository _voucherDetailRepository;
    private readonly IVoucherTafsiliLevelGuard _tafsiliLevelGuard;
    private readonly ITreasurySettingReadRepository _settingReadRepository;
    private readonly IBankAccountReadRepository _bankAccountReadRepository;
    private readonly IPaymentRequestPayablesTafsiliResolver _payablesTafsiliResolver;
    private readonly ICurrentUser _currentUser;

    public PaymentRequestPaymentVoucherBuilder(
        IVoucherHeadRepository voucherHeadRepository,
        IVoucherDetailRepository voucherDetailRepository,
        IVoucherTafsiliLevelGuard tafsiliLevelGuard,
        ITreasurySettingReadRepository settingReadRepository,
        IBankAccountReadRepository bankAccountReadRepository,
        IPaymentRequestPayablesTafsiliResolver payablesTafsiliResolver,
        ICurrentUser currentUser)
    {
        _voucherHeadRepository = voucherHeadRepository;
        _voucherDetailRepository = voucherDetailRepository;
        _tafsiliLevelGuard = tafsiliLevelGuard;
        _settingReadRepository = settingReadRepository;
        _bankAccountReadRepository = bankAccountReadRepository;
        _payablesTafsiliResolver = payablesTafsiliResolver;
        _currentUser = currentUser;
    }

    public async Task<PaymentRequestVoucherBuildResult> BuildAndStageAsync(
        TB_TR_PAYMENT_REQUEST paymentRequest, string paidDate, string vahedCode, CancellationToken cancellationToken = default)
    {
        var setting = await _settingReadRepository.GetByVahedAsync(vahedCode, cancellationToken)
            ?? throw new PaymentRequestSettingsMissingException(vahedCode);

        var payablesAccountId = setting.PayablesAccountId
            ?? throw new PaymentRequestTreasurySettingAccountMissingException(PayablesAccountLabel);

        var bankAccount = await _bankAccountReadRepository.GetByIdAsync(paymentRequest.PAYMENT_ACCOUNT_ID, vahedCode, cancellationToken)
            ?? throw new NotFoundException("BankAccount", paymentRequest.PAYMENT_ACCOUNT_ID);

        var bankAccountCodeId = bankAccount.AccountCodeId
            ?? throw new NotFoundException("BankAccountCode", paymentRequest.PAYMENT_ACCOUNT_ID);

        var bankLinks = bankAccount.TafsiliLinks
            .Select(l => new VoucherDetailTafsiliLinkInput(l.TafsiliId, l.LevelId))
            .ToList();

        var payablesLinks = await _payablesTafsiliResolver.ResolvePayablesLinksAsync(
            payablesAccountId, paymentRequest.ID, paymentRequest.BENEFICIARY_TAFSILI_ID, cancellationToken);

        await EnsureTafsiliSatisfiedAsync(payablesAccountId, payablesLinks, cancellationToken);
        await EnsureTafsiliSatisfiedAsync(bankAccountCodeId, bankLinks, cancellationToken);

        var year = paidDate.Length >= 4 ? paidDate[..4] : paidDate;
        var docNum = await _voucherHeadRepository.GetNextDocNumAsync(vahedCode, year, cancellationToken);

        var now = DateTime.UtcNow;
        var userId = _currentUser.UserId;

        var head = new TB_VOUCHERSHEAD
        {
            ID = Guid.NewGuid(),
            DOC_NUM = docNum,
            DATE_DOC = paidDate,
            DOCLIFE = DocLife.Temporary,
            HEAD_DESC = $"پرداخت — {paymentRequest.CODE} — {paymentRequest.BENEFICIARY_NAME}",
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
            ACCOUNT_ID = payablesAccountId,
            DESCRIPTION = "پرداخت — بستانکاران",
            RADIF = 1,
            DEBTOR = paymentRequest.NET_PAYABLE_AMOUNT,
            CREDITOR = 0,
            VAHEDCODE = vahedCode,
            YEAR = year,
            ADDUSERID = userId,
            CREATEDDATE = now,
            ISDELETED = false,
        };

        await _voucherDetailRepository.AddAsync(debitDetail, cancellationToken);
        await AddTafsiliLinksAsync(debitDetail.ID, payablesLinks, vahedCode, year, now, userId, cancellationToken);
        lines.Add(new PaymentRequestVoucherLineResult(payablesAccountId, paymentRequest.NET_PAYABLE_AMOUNT, 0, payablesLinks));

        var creditDetail = new TB_VOUCHERSDETAIL
        {
            ID = Guid.NewGuid(),
            VOUCHERSHEAD_ID = head.ID,
            ACCOUNT_ID = bankAccountCodeId,
            DESCRIPTION = "پرداخت — بانک",
            RADIF = 2,
            DEBTOR = 0,
            CREDITOR = paymentRequest.NET_PAYABLE_AMOUNT,
            VAHEDCODE = vahedCode,
            YEAR = year,
            ADDUSERID = userId,
            CREATEDDATE = now,
            ISDELETED = false,
        };

        await _voucherDetailRepository.AddAsync(creditDetail, cancellationToken);
        await AddTafsiliLinksAsync(creditDetail.ID, bankLinks, vahedCode, year, now, userId, cancellationToken);
        lines.Add(new PaymentRequestVoucherLineResult(bankAccountCodeId, 0, paymentRequest.NET_PAYABLE_AMOUNT, bankLinks));

        // No balance assertion needed here (unlike voucher 1): both lines are literally
        // `paymentRequest.NET_PAYABLE_AMOUNT` above, so debit == credit by construction, not by
        // summing independent fields that could drift apart.
        return new PaymentRequestVoucherBuildResult(head.ID, docNum, paidDate, year, lines);
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
            throw new PaymentRequestVoucherTafsiliMissingException(accountCodeId, ex.PublicDetail);
        }
    }
}
