using Accounting.Application.AccountCodes.Queries.GetTafsiliLevels;
using Accounting.Application.Common.Exceptions;
using Accounting.Application.Common.Interfaces;

namespace Accounting.Application.Vouchers.Commands.Common;

/// <summary>
/// Enforces «تفصیلی الزامی» on the voucher write path — the rule that has been open since the
/// project began (risk #12) because the rich model that carried it was deliberately dropped, and
/// the Legacy mechanism replacing it was understood but never rebuilt.
///
/// <b>Both halves, because the rule is two-sided.</b> The reference project raises
/// <c>LevelNotProvidedException</c> and <c>UnauthorizedLevelException</c> from three independent
/// write paths (<c>docs/centralaccount-business-reference.md</c> §3-3 and the phase-17 findings),
/// and an implementation with only the first half is worse than it looks: with no FK on
/// <c>TB_VOUCHERDETAIL_LINK_TAFSILI.LEVEL_ID</c>, a تفصیلی written at an unconfigured level is
/// accepted, stored, and then silently disagrees with the معین's configuration in every report.
///
/// <b>Why this could not be a FluentValidation validator.</b> The rule is not a property
/// constraint: answering it requires reading <c>TB_ACCOUNT_LINK_LEVEL</c> for the line's معین.
/// <c>VoucherDetailTafsiliLinkInputValidator</c> stays purely syntactic, as its own doc says.
///
/// <b>This closes the invariant only for the paths that call it.</b> Team rule #1 says the check
/// must exist in Domain/Application and not only in the UI; it now does, for every voucher write
/// path in this project. It says nothing about rows already in the database, which were written
/// before the rule existed — see <c>docs/open-decisions.md</c> «فاز ۳۵» for what that means in
/// practice.
/// </summary>
public sealed class VoucherTafsiliLevelGuard : IVoucherTafsiliLevelGuard
{
    private readonly ITafsiliLookupReadRepository _tafsiliLookupReadRepository;

    /// <summary>
    /// Memoises the level lookup per معین for the lifetime of one request.
    ///
    /// The composite create path (<c>CreateVoucherHeadCommand.InitialDetails</c>) checks every
    /// line, and a real voucher usually posts many lines against the same handful of معین — without
    /// this it would issue one database read per line instead of one per distinct حساب. Safe
    /// because the instance is scoped to a single request: it can never serve a configuration
    /// change made by another caller.
    /// </summary>
    private readonly Dictionary<Guid, IReadOnlyList<TafsiliLevelDto>> _levelsByAccount = new();

    public VoucherTafsiliLevelGuard(ITafsiliLookupReadRepository tafsiliLookupReadRepository)
    {
        _tafsiliLookupReadRepository = tafsiliLookupReadRepository;
    }

    /// <summary>
    /// Throws when <paramref name="tafsiliLinks"/> is not a valid تفصیلی assignment for
    /// <paramref name="accountCodeId"/>.
    /// </summary>
    /// <param name="accountCodeId">
    /// The line's <c>ACCOUNT_ID</c>. <see langword="null"/> is allowed — the column is nullable, and
    /// a line with no حساب has nothing to require. Any تفصیلی sent for such a line is still
    /// rejected: there is no configuration that could permit it.
    /// </param>
    /// <param name="tafsiliLinks">
    /// The تفصیلی the line will have <b>after</b> this request, not the delta. Callers on the
    /// update path have to resolve that themselves, because a null list there means "leave the
    /// existing assignments alone" rather than "no assignments".
    /// </param>
    /// <exception cref="RequiredTafsiliLevelMissingException">A configured level has no value.</exception>
    /// <exception cref="TafsiliLevelNotPermittedException">A value was sent for an unconfigured level.</exception>
    public async Task EnsureSatisfiedAsync(
        Guid? accountCodeId,
        IReadOnlyCollection<VoucherDetailTafsiliLinkInput> tafsiliLinks,
        CancellationToken cancellationToken = default)
    {
        if (accountCodeId is null)
        {
            if (tafsiliLinks.Count > 0)
            {
                throw new TafsiliLevelNotPermittedException(Guid.Empty, tafsiliLinks.First().LevelId);
            }

            return;
        }

        var activeLevels = await GetActiveLevelsAsync(accountCodeId.Value, cancellationToken);

        if (activeLevels.Count == 0 && tafsiliLinks.Count == 0)
        {
            return;
        }

        var activeLevelIds = activeLevels.Select(level => level.LevelId).ToHashSet();
        var providedLevelIds = tafsiliLinks.Select(link => link.LevelId).ToHashSet();

        // Rule A first: a missing required level is what an accountant filling the form actually
        // hits, and naming the levels they still have to fill is the more useful message when both
        // halves are violated at once.
        var missingLevelNames = activeLevels
            .Where(level => !providedLevelIds.Contains(level.LevelId))
            .Select(level => level.LevelName)
            .ToList();

        if (missingLevelNames.Count > 0)
        {
            throw new RequiredTafsiliLevelMissingException(accountCodeId.Value, missingLevelNames);
        }

        // Not FirstOrDefault: its "nothing matched" answer is Guid.Empty, which is also a value a
        // caller can send, so the two cases would be indistinguishable.
        foreach (var levelId in providedLevelIds)
        {
            if (!activeLevelIds.Contains(levelId))
            {
                throw new TafsiliLevelNotPermittedException(accountCodeId.Value, levelId);
            }
        }
    }

    private async Task<IReadOnlyList<TafsiliLevelDto>> GetActiveLevelsAsync(
        Guid accountCodeId,
        CancellationToken cancellationToken)
    {
        if (_levelsByAccount.TryGetValue(accountCodeId, out var cached))
        {
            return cached;
        }

        var levels = await _tafsiliLookupReadRepository.GetActiveLevelsAsync(accountCodeId, cancellationToken);
        _levelsByAccount[accountCodeId] = levels;

        return levels;
    }
}
