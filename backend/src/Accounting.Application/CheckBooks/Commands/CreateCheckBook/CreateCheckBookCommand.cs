using System.Text.Json.Serialization;
using Accounting.Application.Common.Security;
using Accounting.Domain.ValueObjects;
using MediatR;

namespace Accounting.Application.CheckBooks.Commands.CreateCheckBook;

/// <summary>
/// Creates a new <c>TB_CHECKBOOK</c> row (Legacy bank checkbook master). Carries primitive
/// fields only — the handler is responsible for constructing the Domain entity. Returns the
/// newly generated <see cref="Guid"/> ID.
///
/// <c>AccountId</c> is backed by a real, required FK (<c>FK_CHECKBOOK_ACCOUNT</c> to
/// <c>TB_ACCOUNT</c>); <c>CheckTypeId</c> is backed by a real, optional FK
/// (<c>FK_CHECKTYPE</c> to <c>TB_CHECK_TYPE</c>). Both are mapped centrally to 400 by
/// <c>UnitOfWork.SaveChangesAsync</c> on violation.
///
/// <c>CheckBookType</c> — <see cref="CheckType"/> (1=Sori/چک صوری, 2=Real/چک واقعی). Resolved from
/// the project-wide <c>bool?</c>/enum scaffolding bug (CLAUDE.md open risk #2) in phase 27 batch 2
/// — see <c>docs/centralaccount-business-reference.md</c> §24-1 row 19. ⚠️ §24-3 caveat, still
/// open: in the reference project this column is a hardcoded server-side constant
/// (<c>CheckType.real</c>) on both Create and Update and is never a caller input at all. This
/// batch fixed only the CLR type — the field remains caller-supplied here, deliberately not
/// changed alongside the type fix.
///
/// ⚠️ The child <c>TB_CHECK</c> table is permanently embedded (mirrors the reference project's
/// <c>AddCheckPapers</c> builder method) — this project builds no cascade to it. Soft-deleting a
/// checkbook (<c>DeleteCheckBookCommand</c>) leaves its <c>TB_CHECK</c> rows active — a known
/// gap, not fixed here.
/// </summary>
/// <param name="AccountId">Required link to <c>TB_ACCOUNT</c> (<c>FK_CHECKBOOK_ACCOUNT</c>).</param>
/// <param name="CheckBookTitle">Optional checkbook title (max 100 chars).</param>
/// <param name="CheckBookDate">Checkbook issue date, Legacy string format (required, max 8 chars).</param>
/// <param name="FromCheckNumber">First cheque number in the book (required, max 14 chars); part of <c>UK_CHECKBOOK</c>.</param>
/// <param name="ToCheckNumber">Last cheque number in the book (required, max 14 chars); part of <c>UK_CHECKBOOK</c>.</param>
/// <param name="CheckTypeId">Optional link to <c>TB_CHECK_TYPE</c> (<c>FK_CHECKTYPE</c>).</param>
/// <param name="CheckBookType">CHECKBOOK_TYPE column — see the class XML doc for the resolved-enum note and the still-open §24-3 caveat.</param>
/// <param name="Serial">Optional checkbook serial number (max 20 chars).</param>
public sealed record CreateCheckBookCommand(
    Guid AccountId,
    string? CheckBookTitle,
    string CheckBookDate,
    string FromCheckNumber,
    string ToCheckNumber,
    Guid? CheckTypeId,
    CheckType? CheckBookType,
    string? Serial) : IRequest<Guid>, IVahedScopedCommand
{
    /// <summary>
    /// Organizational unit code (required, max 4 chars; part of <c>UK_CHECKBOOK</c>). Never bound
    /// from the request body — <see cref="JsonIgnoreAttribute"/> keeps it out of both model
    /// binding and the Swagger schema — and never trusted even if a caller manages to set it:
    /// <c>VahedScopeBehavior</c> unconditionally overwrites this with the authenticated caller's
    /// own unit code before the request reaches <c>CreateCheckBookCommandHandler</c>. See
    /// <see cref="IVahedScopedCommand"/> for the full mechanism.
    /// </summary>
    [JsonIgnore]
    public string VahedCode { get; set; } = string.Empty;
}
