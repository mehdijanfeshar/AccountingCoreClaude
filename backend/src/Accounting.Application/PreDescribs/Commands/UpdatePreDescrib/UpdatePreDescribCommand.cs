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
/// </summary>
/// <param name="Id">The <c>TB_PREDESCRIB.ID</c> to update (bound from the route, never the body).</param>
/// <param name="AccountId">Optional link to <c>TB_ACCOUNTCODE</c> (<c>FK_ACCOUNT</c>).</param>
/// <param name="Descrip">Description text (max 200 chars).</param>
/// <param name="VahedCode">Optional organizational unit code (max 4 chars).</param>
/// <param name="FlagVoucher">FLAGVOUCHER column — Oracle comment "head=0 Detail=1".</param>
public sealed record UpdatePreDescribCommand(
    Guid Id,
    Guid? AccountId,
    string? Descrip,
    string? VahedCode,
    bool? FlagVoucher) : IRequest;
