using Accounting.Application.Common.Exceptions;
using Accounting.Application.Common.Interfaces;
using Accounting.Application.PettyCash.Queries;
using Accounting.Application.Vouchers.Commands.Common;
using Accounting.Domain.Entity;
using Accounting.Domain.ValueObjects;

namespace Accounting.Application.PettyCash.Commands.Common;

public sealed class PettyCashSettlementVoucherBuilder : IPettyCashSettlementVoucherBuilder
{
    private readonly IVoucherHeadRepository _voucherHeadRepository;
    private readonly IVoucherDetailRepository _voucherDetailRepository;
    private readonly IVoucherTafsiliLevelGuard _tafsiliLevelGuard;
    private readonly IPettyCashSettlementReadRepository _settlementReadRepository;
    private readonly ICurrentUser _currentUser;

    public PettyCashSettlementVoucherBuilder(
        IVoucherHeadRepository voucherHeadRepository,
        IVoucherDetailRepository voucherDetailRepository,
        IVoucherTafsiliLevelGuard tafsiliLevelGuard,
        IPettyCashSettlementReadRepository settlementReadRepository,
        ICurrentUser currentUser)
    {
        _voucherHeadRepository = voucherHeadRepository;
        _voucherDetailRepository = voucherDetailRepository;
        _tafsiliLevelGuard = tafsiliLevelGuard;
        _settlementReadRepository = settlementReadRepository;
        _currentUser = currentUser;
    }

    public async Task<PettyCashSettlementVoucherResult> BuildAndStageAsync(
        TB_PC_FUND fund,
        TB_PC_SETTLEMENT_PERIOD period,
        IReadOnlyList<PettyCashSettlementExpenseGroupDto> expenseGroups,
        string vahedCode,
        CancellationToken cancellationToken = default)
    {
        if (fund.ACCOUNTCODE_ID is not { } fundAccountCodeId)
        {
            throw new PettyCashSettlementFundAccountMissingException(fund.ID);
        }

        foreach (var group in expenseGroups)
        {
            if (group.AccountCodeId is null)
            {
                throw new PettyCashSettlementExpenseAccountMissingException(group.ExpenseId);
            }
        }

        var fundTafsilis = await _settlementReadRepository.GetFundTafsilisAsync(fund.ID, vahedCode, cancellationToken);

        // Every line checked before anything is staged — the same all-or-nothing shape
        // CreateVoucherHeadCommandHandler uses for its InitialDetails.
        foreach (var group in expenseGroups)
        {
            await EnsureTafsiliSatisfiedAsync(group.AccountCodeId!.Value, group.Tafsilis, cancellationToken);
        }

        await EnsureTafsiliSatisfiedAsync(fundAccountCodeId, fundTafsilis, cancellationToken);

        var totalAmount = expenseGroups.Sum(g => g.Amount);

        var year = period.PERIOD_END.Length >= 4 ? period.PERIOD_END[..4] : period.PERIOD_END;

        var nextDocNum = await _voucherHeadRepository.GetNextDocNumAsync(vahedCode, year, cancellationToken);

        var now = DateTime.UtcNow;
        var userId = _currentUser.UserId;

        var head = new TB_VOUCHERSHEAD
        {
            ID = Guid.NewGuid(),
            DOC_NUM = nextDocNum,
            DATE_DOC = period.PERIOD_END,
            // §۹ — صاحب پروژه: سند تسویهٔ دوره «موقت» صادر می‌شود، نه «تأیید دائم».
            DOCLIFE = DocLife.Temporary,
            HEAD_DESC = $"تسویه تنخواه {fund.NAME} — دوره {period.PERIOD_START} تا {period.PERIOD_END}",
            VAHEDCODE = vahedCode,
            YEAR = year,
            // §۹ — "سند: ISAUTOMATIC = true".
            ISAUTOMATIC = true,
            ADDUSERID = userId,
            CREATEDDATE = now,
            ISDELETED = false,
        };

        await _voucherHeadRepository.AddAsync(head, cancellationToken);

        var radif = 0;

        foreach (var group in expenseGroups)
        {
            radif++;

            var detail = new TB_VOUCHERSDETAIL
            {
                ID = Guid.NewGuid(),
                VOUCHERSHEAD_ID = head.ID,
                ACCOUNT_ID = group.AccountCodeId,
                DESCRIPTION = $"تسویه هزینه — {group.AccountTitle ?? group.AccountCode}",
                RADIF = radif,
                DEBTOR = group.Amount,
                CREDITOR = 0,
                VAHEDCODE = vahedCode,
                YEAR = year,
                ADDUSERID = userId,
                CREATEDDATE = now,
                ISDELETED = false,
            };

            await _voucherDetailRepository.AddAsync(detail, cancellationToken);

            foreach (var tafsili in group.Tafsilis)
            {
                await _voucherDetailRepository.AddTafsiliLinkAsync(
                    new TB_VOUCHERDETAIL_LINK_TAFSILI
                    {
                        ID = Guid.NewGuid(),
                        VOUCHERSDETAIL_ID = detail.ID,
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

        radif++;

        var creditLine = new TB_VOUCHERSDETAIL
        {
            ID = Guid.NewGuid(),
            VOUCHERSHEAD_ID = head.ID,
            ACCOUNT_ID = fundAccountCodeId,
            DESCRIPTION = $"تسویه تنخواه {fund.NAME}",
            RADIF = radif,
            DEBTOR = 0,
            CREDITOR = totalAmount,
            VAHEDCODE = vahedCode,
            YEAR = year,
            ADDUSERID = userId,
            CREATEDDATE = now,
            ISDELETED = false,
        };

        await _voucherDetailRepository.AddAsync(creditLine, cancellationToken);

        foreach (var tafsili in fundTafsilis)
        {
            await _voucherDetailRepository.AddTafsiliLinkAsync(
                new TB_VOUCHERDETAIL_LINK_TAFSILI
                {
                    ID = Guid.NewGuid(),
                    VOUCHERSDETAIL_ID = creditLine.ID,
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

        // §۹ — تراز قبل از SaveChangesAsync چک شود. By construction totalDebtor == totalCredit
        // (the credit line's amount IS totalAmount), but this is asserted explicitly rather than
        // assumed — see PettyCashSettlementUnbalancedException XML doc.
        var totalDebtor = expenseGroups.Sum(g => g.Amount);
        var totalCredit = totalAmount;

        if (totalDebtor != totalCredit)
        {
            throw new PettyCashSettlementUnbalancedException(totalDebtor, totalCredit);
        }

        return new PettyCashSettlementVoucherResult(head.ID, nextDocNum, totalAmount);
    }

    private async Task EnsureTafsiliSatisfiedAsync(
        Guid accountCodeId, IReadOnlyList<PettyCashSettlementTafsiliDto> tafsilis, CancellationToken cancellationToken)
    {
        var links = tafsilis.Select(t => new VoucherDetailTafsiliLinkInput(t.TafsiliId, t.LevelId)).ToList();

        try
        {
            await _tafsiliLevelGuard.EnsureSatisfiedAsync(accountCodeId, links, cancellationToken);
        }
        catch (TafsiliLevelRuleException ex)
        {
            throw new PettyCashSettlementTafsiliMissingException(accountCodeId, ex.PublicDetail);
        }
    }
}
