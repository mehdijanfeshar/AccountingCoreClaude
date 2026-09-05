using MediatR;

namespace Accounting.Application.CheckBooks.Commands.UpdateCheckBook;

/// <summary>
/// Fully replaces the writable fields of an existing <c>TB_CHECKBOOK</c> row (PUT semantics, not
/// PATCH) — the same replace-vs-patch rationale as <c>UpdateWorkShopCommand</c> applies here.
///
/// Deliberately excludes <c>ID</c>, <c>ADDUSERID</c>, <c>CREATEDDATE</c> and <c>ISDELETED</c>:
/// identity and creation audit are immutable after insert, and <c>ISDELETED</c> is owned
/// exclusively by <c>DeleteCheckBookCommand</c>. <c>CHANGEUSERID</c>/<c>UPDATEDDATE</c> are
/// likewise absent because the handler sources them from
/// <see cref="Accounting.Application.Common.Interfaces.ICurrentUser"/> and the server clock,
/// never from client input.
/// </summary>
/// <param name="Id">The <c>TB_CHECKBOOK.ID</c> to update (bound from the route, never the body).</param>
/// <param name="AccountId">Required link to <c>TB_ACCOUNT</c> (<c>FK_CHECKBOOK_ACCOUNT</c>).</param>
/// <param name="CheckBookTitle">Optional checkbook title (max 100 chars).</param>
/// <param name="CheckBookDate">Checkbook issue date, Legacy string format (required, max 8 chars).</param>
/// <param name="FromCheckNumber">First cheque number in the book (required, max 14 chars); part of <c>UK_CHECKBOOK</c>.</param>
/// <param name="ToCheckNumber">Last cheque number in the book (required, max 14 chars); part of <c>UK_CHECKBOOK</c>.</param>
/// <param name="CheckTypeId">Optional link to <c>TB_CHECK_TYPE</c> (<c>FK_CHECKTYPE</c>).</param>
/// <param name="VahedCode">Organizational unit code (required, max 4 chars); part of <c>UK_CHECKBOOK</c>.</param>
/// <param name="CheckBookType">CHECKBOOK_TYPE column — see <c>CreateCheckBookCommand</c> XML doc for the unverified-enum note.</param>
/// <param name="Serial">Optional checkbook serial number (max 20 chars).</param>
public sealed record UpdateCheckBookCommand(
    Guid Id,
    Guid AccountId,
    string? CheckBookTitle,
    string CheckBookDate,
    string FromCheckNumber,
    string ToCheckNumber,
    Guid? CheckTypeId,
    string VahedCode,
    bool? CheckBookType,
    string? Serial) : IRequest;
