using Accounting.Application.Common.Exceptions;
using Accounting.Application.Common.Interfaces;
using Accounting.Domain.Entity;
using Accounting.Domain.ValueObjects;

namespace Accounting.Application.Treasury.Commands.Common;

/// <summary>See <see cref="IReceiptRegistrationService"/> XML doc.</summary>
public sealed class ReceiptRegistrationService : IReceiptRegistrationService
{
    private readonly ITreasuryTreasurerAuthorizer _treasurerAuthorizer;
    private readonly IReceiptVoucherBuilder _voucherBuilder;
    private readonly IPayReciveHeadRepository _payReciveHeadRepository;
    private readonly ICurrentUser _currentUser;

    public ReceiptRegistrationService(
        ITreasuryTreasurerAuthorizer treasurerAuthorizer,
        IReceiptVoucherBuilder voucherBuilder,
        IPayReciveHeadRepository payReciveHeadRepository,
        ICurrentUser currentUser)
    {
        _treasurerAuthorizer = treasurerAuthorizer;
        _voucherBuilder = voucherBuilder;
        _payReciveHeadRepository = payReciveHeadRepository;
        _currentUser = currentUser;
    }

    public async Task RegisterAsync(TB_TR_RECEIPT receipt, string vahedCode, CancellationToken cancellationToken = default)
    {
        if (receipt.STATE != ReceiptState.Draft)
        {
            throw new TreasuryReceiptStateConflictException(receipt.ID, receipt.STATE, "پیش‌نویس");
        }

        await _treasurerAuthorizer.EnsureTreasurerAsync(vahedCode, cancellationToken);

        var voucherResult = await _voucherBuilder.BuildAndStageAsync(receipt, vahedCode, cancellationToken);

        var now = DateTime.UtcNow;
        var userId = _currentUser.UserId;

        var nextCode = await _payReciveHeadRepository.GetNextCodeAsync(vahedCode, voucherResult.Year, cancellationToken);

        var payRecivHead = new TB_PAYRECIVHEAD
        {
            ID = Guid.NewGuid(),
            PAYRECIVCODE = nextCode.ToString("00000"),
            PAYRECIVDATE = receipt.RECEIPT_DATE,
            PAYRECIVDESCRIPTION = $"دریافت — {receipt.CODE} — {receipt.PAYER_NAME}",
            PAYRECIVTYPE = PayRecivType.Recive,
            VAHEDCODE = vahedCode,
            YEAR = voucherResult.Year,
            VOUCHERSHEAD_ID = voucherResult.VoucherHeadId,
            ADDUSERID = userId,
            CREATEDDATE = now,
            ISDELETED = false,
        };

        await _payReciveHeadRepository.AddAsync(payRecivHead, cancellationToken);

        var radif = 0;

        foreach (var line in voucherResult.Lines)
        {
            radif++;

            var detail = new TB_PAYRECIVDETAIL
            {
                ID = Guid.NewGuid(),
                ACCOUNTCODE_ID = line.AccountCodeId,
                ARTICLEDESCRIPTION = line.Debit > 0 ? "دریافت — بانک" : "دریافت — حساب‌های دریافتنی",
                RADIF = radif,
                DEBTOR = line.Debit,
                CREDITOR = line.Credit,
                VAHEDCODE = vahedCode,
                YEAR = voucherResult.Year,
                PAYRECIVHEAD_ID = payRecivHead.ID,
                ADDUSERID = userId,
                CREATEDDATE = now,
                ISDELETED = false,
            };

            await _payReciveHeadRepository.AddDetailAsync(detail, cancellationToken);

            foreach (var tafsili in line.TafsiliLinks)
            {
                await _payReciveHeadRepository.AddDetailTafsiliLinkAsync(
                    new TB_PAYRECIVDETAIL_LINK_TAFSILI
                    {
                        ID = Guid.NewGuid(),
                        PAYRECIVDETAIL_ID = detail.ID,
                        TAFSILI_ID = tafsili.TafsiliId,
                        LEVEL_ID = tafsili.LevelId,
                        VAHEDCODE = vahedCode,
                        YEAR = voucherResult.Year,
                        ADDUSERID = userId,
                        CREATEDDATE = now,
                        ISDELETED = false,
                    },
                    cancellationToken);
            }
        }

        receipt.VOUCHER_ID = voucherResult.VoucherHeadId;
        receipt.PAYRECIVHEAD_ID = payRecivHead.ID;
        receipt.STATE = ReceiptState.Registered;
        receipt.REGISTERED_BY = userId;
        receipt.REGISTERED_DATE = now;
        receipt.CHANGEUSERID = userId;
        receipt.UPDATEDDATE = now;
    }
}
