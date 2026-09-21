using Accounting.Application.Common.Interfaces;
using Accounting.Domain.Entity;

namespace Accounting.Application.Accounts.Commands.Common;

/// <summary>
/// Keeps <c>TB_ACCOUNT_LINK_LEVEL</c> (which تفصیلی levels a معین requires when a voucher is
/// posted) in step with <c>TB_ACCOUNT_LINK_TAFSILGROUP</c> (which گروه تفصیلی supplies the values
/// for each level).
///
/// <b>Why this exists.</b> The two tables answer different questions and both are legitimate
/// (<c>docs/centralaccount-business-reference.md</c> §3-3), but until now only the second one had
/// a write path in this system: the «ارتباط معین با گروه تفصیلی» screen wrote
/// <c>TB_ACCOUNT_LINK_TAFSILGROUP</c>, while the voucher form read its active levels from
/// <c>TB_ACCOUNT_LINK_LEVEL</c> — a table nothing here could edit. Nothing kept them in
/// agreement, and on real data they disagreed: معین <c>005000</c> had groups linked for levels 1
/// and 3 while the voucher form demanded levels 3 and 4. Level 4 then rendered as a required
/// field whose dropdown can never be populated (no group ⇒ no candidate تفصیلی), which blocks
/// voucher entry outright, and level 1 — configured, with a group — was never asked for at all.
///
/// <b>The rule this encodes</b> (project-owner decision, 2026-09-21): a level is active/required
/// for a معین exactly when at least one live گروه تفصیلی link exists for that (معین, level) pair.
/// The reference project reaches the same table from the معین form instead — its
/// <c>AddMoinCodeCommand</c>/<c>UpdateMoinCodeCommand</c> carry a <c>LevelIds</c> list — but its
/// meaning of a row is identical to ours: <i>existence of the row = the level is both allowed and
/// required</i>, with no "allowed but optional" state anywhere in this schema.
///
/// <b>Scope of a sync is the whole معین, not just the level that was touched.</b> That is what
/// repairs rows like <c>005000</c>'s level 4: a level with no live group link is retired the next
/// time anything on that معین's links is saved. It is a real consequence and worth being explicit
/// about — a legacy level row nobody asked to change can disappear from a save that never
/// mentioned it. It is a soft delete, so the row and its audit trail survive, and the state being
/// retired is the unsatisfiable one (a required level that no تفصیلی can ever satisfy), never a
/// working configuration.
/// </summary>
public sealed class AccountLevelLinkSynchronizer
{
    private readonly IAccountCodeRepository _accountCodeRepository;
    private readonly ICurrentUser _currentUser;

    public AccountLevelLinkSynchronizer(IAccountCodeRepository accountCodeRepository, ICurrentUser currentUser)
    {
        _accountCodeRepository = accountCodeRepository;
        _currentUser = currentUser;
    }

    /// <summary>
    /// Brings <paramref name="accountCodeId"/>'s level rows in line with its گروه تفصیلی links.
    ///
    /// Call this <b>after</b> staging the link change and <b>before</b>
    /// <see cref="IUnitOfWork.SaveChangesAsync"/> — everything it stages belongs to the caller's
    /// transaction, so a link change and the level rows it implies commit together or not at all.
    /// It deliberately does not save: three write paths share it, and each one owns its own
    /// transaction boundary.
    ///
    /// Idempotent. Running it on a معین that already agrees stages nothing, which is what lets
    /// every write path call it unconditionally instead of working out whether it needs to.
    /// </summary>
    public async Task SyncAsync(Guid accountCodeId, CancellationToken cancellationToken = default)
    {
        var groupLinks = await _accountCodeRepository.GetTafsilGroupLinksForSyncAsync(accountCodeId, cancellationToken);
        var levelLinks = await _accountCodeRepository.GetLevelLinksForSyncAsync(accountCodeId, cancellationToken);

        var requiredLevelIds = groupLinks
            .Where(link => !link.ISDELETED)
            .Select(link => link.LEVEL_ID)
            .ToHashSet();

        var userId = _currentUser.UserId;
        var now = DateTime.UtcNow;
        var toInsert = new List<TB_ACCOUNT_LINK_LEVEL>();

        foreach (var levelId in requiredLevelIds)
        {
            var rowsForLevel = levelLinks.Where(row => row.LEVEL_ID == levelId).ToList();

            if (rowsForLevel.Any(row => !row.ISDELETED))
            {
                continue;
            }

            // Reviving beats inserting: this table has no unique constraint on
            // (ACCOUNT_ID, LEVEL_ID), so inserting instead would quietly accumulate a second,
            // third, fourth row for the same level every time it is switched off and on again.
            var revivable = rowsForLevel.FirstOrDefault();
            if (revivable is not null)
            {
                revivable.ISDELETED = false;
                revivable.CHANGEUSERID = userId;
                revivable.UPDATEDDATE = now;
                continue;
            }

            toInsert.Add(new TB_ACCOUNT_LINK_LEVEL
            {
                ID = Guid.NewGuid(),
                ACCOUNT_ID = accountCodeId,
                LEVEL_ID = levelId,
                ADDUSERID = userId,
                CREATEDDATE = now,
                ISDELETED = false,
            });
        }

        foreach (var orphan in levelLinks.Where(row => !row.ISDELETED && !requiredLevelIds.Contains(row.LEVEL_ID)))
        {
            orphan.ISDELETED = true;
            orphan.CHANGEUSERID = userId;
            orphan.UPDATEDDATE = now;
        }

        if (toInsert.Count > 0)
        {
            await _accountCodeRepository.AddLevelLinksAsync(toInsert, cancellationToken);
        }
    }
}
