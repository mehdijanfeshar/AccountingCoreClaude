using MediatR;

namespace Accounting.Application.Accounts.Commands.DeleteAccountCode;

/// <summary>
/// Soft-deletes a <c>TB_ACCOUNTCODE</c> row: sets <c>ISDELETED = true</c> plus the
/// <c>CHANGEUSERID</c>/<c>UPDATEDDATE</c> audit columns. Never issues a physical DELETE —
/// <c>TB_ACCOUNTCODE</c> participates in Legacy referential integrity and the read side
/// already filters on <c>ISDELETED != true</c>, so a physical delete would both break FK
/// integrity and diverge from the original Legacy application's behaviour.
/// </summary>
/// <param name="Id">The <c>TB_ACCOUNTCODE.ID</c> to soft-delete (bound from the route).</param>
public sealed record DeleteAccountCodeCommand(Guid Id) : IRequest;
