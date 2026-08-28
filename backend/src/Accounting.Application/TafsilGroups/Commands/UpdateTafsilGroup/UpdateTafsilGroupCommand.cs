using MediatR;

namespace Accounting.Application.TafsilGroups.Commands.UpdateTafsilGroup;

/// <summary>
/// Fully replaces the writable fields of an existing <c>TB_TAFSIL_GROUP</c> row (PUT semantics,
/// not PATCH) — mirrors <c>UpdateAccountCodeCommand</c>. Deliberately excludes <c>ID</c>,
/// <c>ADDUSERID</c>, <c>CREATEDDATE</c> and <c>ISDELETED</c>: identity and creation audit are
/// immutable after insert, and <c>ISDELETED</c> is owned exclusively by
/// <c>DeleteTafsilGroupCommand</c>. <c>CHANGEUSERID</c>/<c>UPDATEDDATE</c> are likewise absent
/// because the handler sources them from
/// <see cref="Accounting.Application.Common.Interfaces.ICurrentUser"/> and the server clock,
/// never from client input.
///
/// Like Create, this can violate <c>UK_TBTAFSILGROUP</c> (<c>TafsilGroupCode</c>/<c>ISDELETED</c>)
/// against a *different* row, surfacing as the same central ORA-00001 → 409 mapping.
/// </summary>
/// <param name="Id">The <c>TB_TAFSIL_GROUP.ID</c> to update (bound from the route, never the body).</param>
/// <param name="TafsilGroupCode">TAFSILGROUP_CODE column (max 3 chars, required).</param>
/// <param name="TafsilGroupName">TAFSILGROUP_NAME column (max 200 chars, required).</param>
/// <param name="PersonType">PERSONTYPE column. See <see cref="Accounting.Application.TafsilGroups.Commands.CreateTafsilGroup.CreateTafsilGroupCommand.PersonType"/> for the unverified-enum caveat.</param>
public sealed record UpdateTafsilGroupCommand(
    Guid Id,
    string TafsilGroupCode,
    string TafsilGroupName,
    bool? PersonType) : IRequest;
