using MediatR;

namespace Accounting.Application.IdentityGroups.Commands.UpdateIdentityGroup;

/// <summary>
/// Fully replaces the writable fields of an existing <c>TB_IDENTITYGROUP</c> row (PUT semantics,
/// not PATCH) — mirrors <c>UpdateAccountCodeCommand</c>. Deliberately excludes <c>ID</c>,
/// <c>ADDUSERID</c>, <c>CREATEDDATE</c> and <c>ISDELETED</c>: identity and creation audit are
/// immutable after insert, and <c>ISDELETED</c> is owned exclusively by
/// <c>DeleteIdentityGroupCommand</c>. <c>CHANGEUSERID</c>/<c>UPDATEDDATE</c> are likewise absent
/// because the handler sources them from
/// <see cref="Accounting.Application.Common.Interfaces.ICurrentUser"/> and the server clock.
/// </summary>
/// <param name="Id">The <c>TB_IDENTITYGROUP.ID</c> to update (bound from the route, never the body).</param>
/// <param name="IdentityGroupsDesc">IDENTITYGROUPS_DESC column (max 100 chars, required).</param>
/// <param name="IdentityGroupsCode">
/// IDENTITYGROUPS_CODE column (max 3 chars, optional — participates in <c>UK_IDENTITYGROUPCODE</c>).
/// </param>
/// <param name="VahedCode">VAHEDCODE column (max 4 chars, required organizational unit code).</param>
/// <param name="TafsiliId">Optional FK to <c>TB_TAFSILI</c> (constraint <c>FK_IDENTITY_TAFSILI</c>).</param>
public sealed record UpdateIdentityGroupCommand(
    Guid Id,
    string IdentityGroupsDesc,
    string? IdentityGroupsCode,
    string VahedCode,
    Guid? TafsiliId) : IRequest;
