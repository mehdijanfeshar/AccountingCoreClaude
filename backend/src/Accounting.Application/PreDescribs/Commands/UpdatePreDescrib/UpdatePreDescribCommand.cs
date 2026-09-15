using System.Text.Json.Serialization;
using Accounting.Application.Common.Security;
using MediatR;

namespace Accounting.Application.PreDescribs.Commands.UpdatePreDescrib;

/// <summary>
/// Fully replaces the writable fields of an existing <c>TB_PREDESCRIB</c> row (PUT semantics,
/// not PATCH) — the same replace-vs-patch rationale as <c>UpdateAccountCodeCommand</c> applies
/// here (every column is nullable).
///
/// Deliberately excludes <c>ID</c> and <c>ADDUSERID</c>: identity and creation audit are
/// immutable after insert. Unlike every other Update command in this project, it ALSO has no
/// <c>CHANGEUSERID</c>/<c>UPDATEDDATE</c> to source from <c>ICurrentUser</c>/the server clock —
/// <c>TB_PREDESCRIB</c> simply has no such audit columns. This is intentional, not an
/// oversight: see <c>UpdatePreDescribCommandHandler</c> XML doc, which does not depend on
/// <see cref="Accounting.Application.Common.Interfaces.ICurrentUser"/> at all.
///
/// ⚠️ <b>Scope note:</b> <see cref="IVahedScopedCommand"/> here only guarantees that
/// <c>VAHEDCODE</c> cannot be *changed* to an arbitrary unit by the caller. It does
/// <b>not</b> check whether the caller is allowed to touch this particular row in the first
/// place — record-ownership verification on Update (i.e. "does this <c>Id</c> already belong to
/// the caller's unit?") is explicitly out of scope for this pass, by project-owner decision. The
/// IDOR risk on direct-by-id access therefore remains open for Update; only the "what unit does
/// this row end up in" half of the problem is closed here.
/// </summary>
/// <param name="Id">The <c>TB_PREDESCRIB.ID</c> to update (bound from the route, never the body).</param>
/// <param name="AccountId">Optional link to <c>TB_ACCOUNTCODE</c> (<c>FK_ACCOUNT</c>).</param>
/// <param name="Descrip">Description text (max 200 chars).</param>
/// <param name="FlagVoucher">FLAGVOUCHER column — Oracle comment "head=0 Detail=1".</param>
public sealed record UpdatePreDescribCommand(
    Guid Id,
    Guid? AccountId,
    string? Descrip,
    bool? FlagVoucher) : IRequest, IVahedScopedCommand
{
    /// <summary>
    /// Organizational unit code (<c>VAHEDCODE</c> column — nullable at the Legacy schema level,
    /// but always populated with a real value here). Never bound from the request body —
    /// <see cref="JsonIgnoreAttribute"/> keeps it out of both model binding and the Swagger
    /// schema — and never trusted even if a caller manages to set it: <c>VahedScopeBehavior</c>
    /// unconditionally overwrites this with the authenticated caller's own unit code before the
    /// request reaches <c>UpdatePreDescribCommandHandler</c>. See <see cref="IVahedScopedCommand"/>
    /// for the full mechanism, and the scope note above for what this does <b>not</b> cover.
    /// </summary>
    [JsonIgnore]
    public string VahedCode { get; set; } = string.Empty;
}
