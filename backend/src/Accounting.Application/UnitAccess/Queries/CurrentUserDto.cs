namespace Accounting.Application.UnitAccess.Queries;

/// <summary>
/// Who the caller is, as the client needs to know them — the new project's equivalent of the
/// reference project's <c>CurrentUser/GetCurrentUser</c>.
///
/// <para>
/// <b>Deliberately a tiny fraction of the reference DTO.</b> There, <c>GetCurrentUserAsync</c>
/// is an outbound HTTP call to the IDP portal
/// (<c>/portal/api/v2.0/users/{nationalCode}/info</c>) that returns ~90 fields including
/// national code, mobile, birth date, postal address and password state. This project's caller
/// identity already arrives on the validated JWT, so none of that round trip — or that
/// disclosure — is needed to render a unit/year picker. If a future feature genuinely needs
/// IDP profile fields, that is a new outbound integration to design, not a field to quietly add
/// here.
/// </para>
/// </summary>
/// <param name="UserId">The authenticated user id (NameIdentifier claim) — the same value written to ADDUSERID.</param>
/// <param name="VahedCode">The caller's own organizational unit code, from the token's org claim.</param>
/// <param name="VahedName">
/// The unit's display name from <c>TB_VAHED_INFO</c>, or <see langword="null"/> when the claim
/// matches no Legacy row. Null is meaningful and is not smoothed over: it says the token and the
/// unit table disagree.
/// </param>
/// <param name="IsHeadquarters">
/// True when the caller's unit type grants blanket access to every unit. The client uses this
/// only to label the picker; it is never the client's decision.
/// </param>
public sealed record CurrentUserDto(
    string UserId,
    string? VahedCode,
    string? VahedName,
    bool IsHeadquarters);
