using MediatR;

namespace Accounting.Application.Accounts.Commands.UpdateAccountTafsilGroupLink;

/// <summary>
/// Fully replaces the <c>LEVEL_ID</c>/<c>TAFSILGROUP_ID</c> of an existing
/// <c>TB_ACCOUNT_LINK_TAFSILGROUP</c> row — mirrors <c>UpdateTafsilGroupCommand</c>'s PUT
/// semantics. Deliberately excludes <c>ID</c>/<c>ACCOUNT_ID</c> (immutable identity of the link
/// and its parent معین), <c>ADDUSERID</c>/<c>CREATEDDATE</c>/<c>ISDELETED</c> (owned elsewhere).
/// See <see cref="Accounting.Application.Accounts.Commands.LinkAccountCodeToTafsilGroup.LinkAccountCodeToTafsilGroupCommand"/>
/// XML doc for why this Command lives under <c>Accounts</c> rather than an independent link-table
/// feature.
/// </summary>
/// <param name="AccountCodeId">TB_ACCOUNTCODE.ID — the معین this link must belong to (bound from the route; scopes the lookup so a caller cannot address a link under a different معین).</param>
/// <param name="LinkId">The <c>TB_ACCOUNT_LINK_TAFSILGROUP.ID</c> to update (bound from the route).</param>
/// <param name="LevelId">TB_LEVEL_TAFSIL.ID — the تفصیلی level this link applies to.</param>
/// <param name="TafsilGroupId">TB_TAFSIL_GROUP.ID — the گروه تفصیلی being linked.</param>
public sealed record UpdateAccountTafsilGroupLinkCommand(
    Guid AccountCodeId,
    Guid LinkId,
    Guid LevelId,
    Guid TafsilGroupId) : IRequest;
