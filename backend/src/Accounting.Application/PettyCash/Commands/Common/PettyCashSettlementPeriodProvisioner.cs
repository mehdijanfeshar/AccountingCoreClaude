using Accounting.Application.Common.Interfaces;
using Accounting.Application.PettyCash.Common;
using Accounting.Domain.Entity;
using Accounting.Domain.ValueObjects;

namespace Accounting.Application.PettyCash.Commands.Common;

public sealed class PettyCashSettlementPeriodProvisioner : IPettyCashSettlementPeriodProvisioner
{
    private readonly IPettyCashSettlementPeriodRepository _periodRepository;
    private readonly ICurrentUser _currentUser;

    public PettyCashSettlementPeriodProvisioner(
        IPettyCashSettlementPeriodRepository periodRepository,
        ICurrentUser currentUser)
    {
        _periodRepository = periodRepository;
        _currentUser = currentUser;
    }

    public async Task<PettyCashSettlementPeriodSnapshot> ComputeCurrentAsync(
        TB_PC_FUND fund, string vahedCode, CancellationToken cancellationToken = default)
    {
        var draft = await _periodRepository.GetDraftAsync(fund.ID, vahedCode, cancellationToken);

        if (draft is not null)
        {
            return new PettyCashSettlementPeriodSnapshot(
                draft.ID, draft.PERIOD_START, draft.PERIOD_END, draft.STATE, draft.OPENING_BALANCE, draft.COUNTED_BALANCE);
        }

        var (start, end, opening) = await ComputeNextAsync(fund, vahedCode, cancellationToken);

        return new PettyCashSettlementPeriodSnapshot(null, start, end, PettyCashSettlementState.Draft, opening, null);
    }

    public async Task<TB_PC_SETTLEMENT_PERIOD> EnsureDraftAsync(
        TB_PC_FUND fund, string vahedCode, CancellationToken cancellationToken = default)
    {
        var existing = await _periodRepository.GetDraftAsync(fund.ID, vahedCode, cancellationToken);

        if (existing is not null)
        {
            return existing;
        }

        var (start, end, opening) = await ComputeNextAsync(fund, vahedCode, cancellationToken);

        var now = DateTime.UtcNow;

        var period = new TB_PC_SETTLEMENT_PERIOD
        {
            ID = Guid.NewGuid(),
            FUND_ID = fund.ID,
            PERIOD_START = start,
            PERIOD_END = end,
            OPENING_BALANCE = opening,
            COUNTED_BALANCE = null,
            STATE = PettyCashSettlementState.Draft,
            VAHEDCODE = vahedCode,
            YEAR = fund.YEAR,
            ADDUSERID = _currentUser.UserId,
            CREATEDDATE = now,
            ISDELETED = false,
        };

        await _periodRepository.AddAsync(period, cancellationToken);

        return period;
    }

    private async Task<(string Start, string End, decimal Opening)> ComputeNextAsync(
        TB_PC_FUND fund, string vahedCode, CancellationToken cancellationToken)
    {
        var lastFinal = await _periodRepository.GetLatestFinalAsync(fund.ID, vahedCode, cancellationToken);

        var (start, end) = PettyCashSettlementPeriodCalculator.GetNextPeriod(
            fund.SETTLEMENT_PERIOD, lastFinal?.PERIOD_END, fund.CREATEDDATE);

        // §۹ — "مانده ابتدای دوره = مانده پایان دورهٔ Final قبلی" (its locked-in COUNTED_BALANCE,
        // never re-derived live); "برای اولین دوره = همان منطق موجودی اولیه (سقف)".
        var opening = lastFinal?.COUNTED_BALANCE ?? fund.CEILING;

        return (start, end, opening);
    }
}
