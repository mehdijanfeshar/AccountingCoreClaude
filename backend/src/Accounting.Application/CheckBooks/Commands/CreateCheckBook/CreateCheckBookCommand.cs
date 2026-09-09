using System.Text.Json.Serialization;
using Accounting.Application.Common.Security;
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
/// ⚠️ <c>CheckBookType</c> is <c>NUMBER(1)</c> typed as <see cref="bool"/>? — this schema has a
/// confirmed track record of <c>NUMBER(1)</c> columns actually being multi-valued enums
/// (CLAUDE.md phase 12; <c>TB_TAFSILI.ISACTIVE</c> is the confirmed dangerous case). Flagged
/// (not fixed) here — the CLR type is deliberately left as <see cref="bool"/>?; re-typing it is a
/// separate, not-yet-made decision.
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
/// <param name="CheckBookType">CHECKBOOK_TYPE column — see the unverified-enum note above.</param>
/// <param name="Serial">Optional checkbook serial number (max 20 chars).</param>
public sealed record CreateCheckBookCommand(
    Guid AccountId,
    string? CheckBookTitle,
    string CheckBookDate,
    string FromCheckNumber,
    string ToCheckNumber,
    Guid? CheckTypeId,
    bool? CheckBookType,
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
