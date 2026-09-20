namespace Accounting.Application.IdentityHeads.Queries;

/// <summary>
/// Read-side projection of <c>TB_IDENTITYHEAD</c> (شناسنامه) together with its fixed-value items.
/// The Domain entity never crosses the Application boundary.
///
/// <para>
/// The fix items are carried inline rather than behind a second request because they are part of
/// this aggregate — an edit form needs them to round-trip, and the list needs at least their
/// count to be useful. See <c>IIdentityHeadRepository</c> for why they have no independent write
/// path.
/// </para>
///
/// ⚠️ Carries no <c>TB_IDENTITYDETAIL</c> data. Those are the <b>variable</b> subgroup values and
/// belong to a voucher line, not to this base-data record.
/// </summary>
/// <param name="Id">ID column.</param>
/// <param name="IdentityGroupId">IDENTITYGROUPS_ID column — FK to <c>TB_IDENTITYGROUP</c>.</param>
/// <param name="IdentityGroupDesc">
/// <c>IDENTITYGROUPS_DESC</c> of the linked group, denormalized through the required navigation
/// so a list row is readable without a second lookup per row.
/// </param>
/// <param name="Serial">SERIAL column — سریال شناسنامه, assigned server-side on create.</param>
/// <param name="FixItems">The fixed-value items of this شناسنامه, one per fixed subgroup.</param>
/// <param name="VahedCode">VAHEDCODE column.</param>
/// <param name="Year">YEAR column.</param>
/// <param name="CreatedDate">Audit trail: creation timestamp.</param>
/// <param name="UpdatedDate">Audit trail: last update timestamp.</param>
/// <param name="AddUserId">Audit trail: creating user identifier.</param>
/// <param name="ChangeUserId">Audit trail: last modifying user identifier.</param>
/// <param name="IsDeleted">Logical-delete flag.</param>
public sealed record IdentityHeadDto(
    Guid Id,
    Guid IdentityGroupId,
    string? IdentityGroupDesc,
    int Serial,
    IReadOnlyList<IdentityHeadFixItemDto> FixItems,
    string VahedCode,
    string Year,
    DateTime? CreatedDate,
    DateTime? UpdatedDate,
    string? AddUserId,
    string? ChangeUserId,
    bool IsDeleted);

/// <summary>
/// One fixed-value item of a شناسنامه — the value recorded for a single fixed subgroup.
/// </summary>
/// <param name="Id">ID column of the <c>TB_IDENTITYFIXITEMS</c> row.</param>
/// <param name="IdentitySubGroupId">IDENTITYSUBGRPS_ID column — which subgroup this value is for.</param>
/// <param name="IdentitySubGroupDesc">
/// <c>SUBGRPS_DESC</c> of that subgroup, denormalized so a form can label the field without
/// fetching every subgroup separately.
/// </param>
/// <param name="Value">FIXITEMS_VALUE column — the recorded value, free text in Legacy.</param>
public sealed record IdentityHeadFixItemDto(
    Guid Id,
    Guid IdentitySubGroupId,
    string? IdentitySubGroupDesc,
    string? Value);
