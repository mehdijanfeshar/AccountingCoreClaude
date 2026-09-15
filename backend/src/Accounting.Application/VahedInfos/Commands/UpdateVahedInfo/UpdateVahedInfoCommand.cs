using MediatR;

namespace Accounting.Application.VahedInfos.Commands.UpdateVahedInfo;

/// <summary>
/// Fully replaces the writable fields of an existing <c>TB_VAHED_INFO</c> row (PUT semantics,
/// not PATCH) — the same replace-vs-patch rationale as <c>UpdateAccountCodeCommand</c> applies
/// here.
///
/// Deliberately excludes only <c>ID</c>: unlike every other Update command in this project,
/// there is no <c>ADDUSERID</c>/<c>CREATEDDATE</c>/<c>ISDELETED</c> to protect and no
/// <c>CHANGEUSERID</c>/<c>UPDATEDDATE</c> to stamp — <c>TB_VAHED_INFO</c> has NO audit columns
/// at all (locked in by
/// <c>Accounting.Application.Tests.VahedInfos.VahedInfoSchemaAssumptionsTests</c>). This
/// handler therefore has no use for <see cref="Accounting.Application.Common.Interfaces.ICurrentUser"/>.
///
/// <c>ParentId</c> is a self-reference into the org-unit hierarchy (<c>PARENT_ID</c>). Unlike
/// Create (where <c>Id</c> is server-generated and cannot yet be known to the caller), Update
/// already knows the row's own <c>Id</c> from the route, so a self-referencing cycle
/// (<c>ParentId == Id</c>) is representable here — guarded by
/// <see cref="UpdateVahedInfoCommandValidator"/>.
/// </summary>
/// <param name="Id">The <c>TB_VAHED_INFO.ID</c> to update (bound from the route, never the body).</param>
/// <param name="VahedCode">Organizational unit code (required, max 4 chars, unique — <c>UK_VAHEDINFO</c>).</param>
/// <param name="VahedName">Organizational unit name (required, max 200 chars).</param>
/// <param name="CityId">Required link to <c>TB_CITY</c> — no mapped FK constraint (unenforced).</param>
/// <param name="VahedTypeId">Required link to <c>TB_VAHED_TYPE</c> (<c>FK_VAHEDINFO_TYPE</c>).</param>
/// <param name="ParentId">Optional parent unit in the self-referencing org-unit hierarchy — must not equal <see cref="Id"/>.</param>
public sealed record UpdateVahedInfoCommand(
    Guid Id,
    string VahedCode,
    string VahedName,
    Guid CityId,
    Guid VahedTypeId,
    Guid? ParentId) : IRequest;
