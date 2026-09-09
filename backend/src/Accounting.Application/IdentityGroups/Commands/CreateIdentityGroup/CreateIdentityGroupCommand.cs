using System.Text.Json.Serialization;
using Accounting.Application.Common.Security;
using MediatR;

namespace Accounting.Application.IdentityGroups.Commands.CreateIdentityGroup;

/// <summary>
/// Creates a new <c>TB_IDENTITYGROUP</c> row (Legacy "identity"/شناسنامه main-group definition,
/// mapped to Oracle table <c>TB_IDENTITYGROUPS</c> — note the singular CLR type name vs. the
/// plural table name). Carries primitive fields only — the handler is responsible for
/// constructing the Domain entity. Returns the newly generated <see cref="Guid"/> ID.
///
/// Uniqueness: the combination <c>(IdentityGroupsCode, VahedCode)</c> is enforced by the Oracle
/// constraint <c>UK_IDENTITYGROUPCODE</c>. No pre-check is performed here — a duplicate
/// combination surfaces as an Oracle ORA-00001, already translated centrally by
/// <c>UnitOfWork.SaveChangesAsync</c> into a
/// <see cref="Accounting.Application.Common.Exceptions.DuplicateKeyException"/> → 409.
/// </summary>
/// <param name="IdentityGroupsDesc">IDENTITYGROUPS_DESC column (max 100 chars, required).</param>
/// <param name="IdentityGroupsCode">
/// IDENTITYGROUPS_CODE column (max 3 chars, optional — participates in <c>UK_IDENTITYGROUPCODE</c>).
/// </param>
/// <param name="TafsiliId">
/// TAFSILI_ID column — optional FK to <c>TB_TAFSILI</c> (constraint <c>FK_IDENTITY_TAFSILI</c>).
/// No pre-check is performed: an id that does not reference an existing <c>TB_TAFSILI</c> row
/// surfaces as an Oracle ORA-02291, already translated centrally by
/// <c>UnitOfWork.SaveChangesAsync</c> into a
/// <see cref="Accounting.Application.Common.Exceptions.ForeignKeyViolationException"/> → 400.
/// </param>
public sealed record CreateIdentityGroupCommand(
    string IdentityGroupsDesc,
    string? IdentityGroupsCode,
    Guid? TafsiliId) : IRequest<Guid>, IVahedScopedCommand
{
    /// <summary>
    /// VAHEDCODE column (max 4 chars, required organizational unit code — participates in
    /// <c>UK_IDENTITYGROUPCODE</c>). Never bound from the request body —
    /// <see cref="JsonIgnoreAttribute"/> keeps it out of both model binding and the Swagger
    /// schema — and never trusted even if a caller manages to set it: <c>VahedScopeBehavior</c>
    /// unconditionally overwrites this with the authenticated caller's own unit code before the
    /// request reaches <c>CreateIdentityGroupCommandHandler</c>. See
    /// <see cref="IVahedScopedCommand"/> for the full mechanism.
    /// </summary>
    [JsonIgnore]
    public string VahedCode { get; set; } = string.Empty;
}
