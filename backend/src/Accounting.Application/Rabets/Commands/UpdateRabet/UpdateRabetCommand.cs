using MediatR;

namespace Accounting.Application.Rabets.Commands.UpdateRabet;

/// <summary>
/// Fully replaces the writable fields of an existing <c>TB_RABET</c> row (PUT semantics, not
/// PATCH) — the same replace-vs-patch rationale as <c>UpdateAccountCodeCommand</c> applies here
/// (every column is nullable).
///
/// Deliberately excludes <c>ID</c>, <c>ADDUSERID</c>, <c>CREATEDDATE</c> and <c>ISDELETED</c>:
/// identity and creation audit are immutable after insert, and <c>ISDELETED</c> is owned
/// exclusively by <c>DeleteRabetCommand</c>. <c>CHANGEUSERID</c>/<c>UPDATEDDATE</c> are likewise
/// absent because the handler sources them from
/// <see cref="Accounting.Application.Common.Interfaces.ICurrentUser"/> and the server clock,
/// never from client input.
/// </summary>
/// <param name="Id">The <c>TB_RABET.ID</c> to update (bound from the route, never the body).</param>
/// <param name="RabetTypeId">Optional link to <c>TB_RABET_TYPE</c> (<c>FK_RABET_TYPE</c>).</param>
/// <param name="AccountCodeId">Optional link to <c>TB_ACCOUNTCODE</c> (<c>FK_RABET_ACCOUNTCODE</c>).</param>
public sealed record UpdateRabetCommand(
    Guid Id,
    Guid? RabetTypeId,
    Guid? AccountCodeId) : IRequest;
