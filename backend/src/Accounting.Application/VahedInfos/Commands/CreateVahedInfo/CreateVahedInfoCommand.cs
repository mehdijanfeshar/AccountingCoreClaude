using MediatR;

namespace Accounting.Application.VahedInfos.Commands.CreateVahedInfo;

/// <summary>
/// Creates a new <c>TB_VAHED_INFO</c> row (Legacy organizational unit / branch). Carries
/// primitive fields only — the handler is responsible for constructing the Domain entity.
/// Returns the newly generated <see cref="Guid"/> ID.
///
/// ⚠️ <c>TB_VAHED_INFO</c> has NO audit columns at all — no <c>ADDUSERID</c>,
/// <c>CHANGEUSERID</c>, <c>CREATEDDATE</c> or <c>UPDATEDDATE</c> (locked in by
/// <c>Accounting.Application.Tests.VahedInfos.VahedInfoSchemaAssumptionsTests</c>). Writes to
/// this table therefore leave NO audit trail whatsoever — a recorded gap, not an oversight; see
/// <c>Accounting.Api.Controllers.VahedInfosController</c> XML doc.
///
/// ⚠️ It also has no <c>ISDELETED</c> column, so this entity gets Create/Update only — no
/// Delete command exists.
///
/// <c>CityId</c>/<c>ParentId</c> carry no surface-level constraint beyond presence/absence:
/// <c>CITY_ID</c> and <c>PARENT_ID</c> have NO mapped FK constraint in <c>LegacyDbContext</c>,
/// so a bad value is written silently (a recorded referential-integrity gap — no pre-check was
/// added, since that would invent a business rule). <c>VahedTypeId</c> IS backed by a real FK
/// (<c>FK_VAHEDINFO_TYPE</c> to <c>TB_VAHED_TYPE</c>), mapped centrally to 400 by
/// <c>UnitOfWork.SaveChangesAsync</c>.
/// </summary>
/// <param name="VahedCode">Organizational unit code (required, max 4 chars, unique — <c>UK_VAHEDINFO</c>).</param>
/// <param name="VahedName">Organizational unit name (required, max 200 chars).</param>
/// <param name="CityId">Required link to <c>TB_CITY</c> — no mapped FK constraint (unenforced).</param>
/// <param name="VahedTypeId">Required link to <c>TB_VAHED_TYPE</c> (<c>FK_VAHEDINFO_TYPE</c>).</param>
/// <param name="ParentId">Optional parent unit in the self-referencing org-unit hierarchy — no mapped FK constraint (unenforced).</param>
public sealed record CreateVahedInfoCommand(
    string VahedCode,
    string VahedName,
    Guid CityId,
    Guid VahedTypeId,
    Guid? ParentId) : IRequest<Guid>;
