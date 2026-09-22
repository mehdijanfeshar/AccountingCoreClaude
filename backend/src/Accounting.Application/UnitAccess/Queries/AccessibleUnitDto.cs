namespace Accounting.Application.UnitAccess.Queries;

/// <summary>
/// One organizational unit the authenticated caller is allowed to act as, as returned by
/// <c>GET /api/me/accessible-units</c> and consumed by the «تغییر سال مالی و واحد» dialog.
///
/// Mirrors the reference project's <c>GetVahedInfoDto</c> (<c>Id</c>/<c>VahedCode</c>/
/// <c>VahedName</c>/<c>IsDefault</c>) so the same dialog shape ports over, plus
/// <see cref="ParentId"/> so a client can render the set as a tree instead of a flat list —
/// the old Angular dialog rendered a flat dropdown and therefore did not need it.
/// </summary>
/// <param name="Id">TB_VAHED_INFO.ID.</param>
/// <param name="VahedCode">TB_VAHED_INFO.VAHEDCODE — the 4-character unit code that becomes the row scope.</param>
/// <param name="VahedName">TB_VAHED_INFO.VAHEDNAME.</param>
/// <param name="ParentId">TB_VAHED_INFO.PARENT_ID — null for a root unit.</param>
/// <param name="IsDefault">
/// True for exactly the caller's own unit (the one their token's org claim names). The reference
/// project's DTO carries the same flag and the old client used it to pre-select the dropdown;
/// keeping it server-computed means the client never has to compare codes itself.
/// </param>
public sealed record AccessibleUnitDto(
    Guid Id,
    string VahedCode,
    string VahedName,
    Guid? ParentId,
    bool IsDefault);
