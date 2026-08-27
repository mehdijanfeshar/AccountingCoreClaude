using MediatR;

namespace Accounting.Application.AccountExceptions.Commands.DeleteAccountException;

/// <summary>
/// Soft-deletes a <c>TB_ACCOUNTEXCEPTION</c> row: sets <c>ISDELETED = true</c> plus the
/// <c>CHANGEUSERID</c>/<c>UPDATEDDATE</c> audit columns. Never issues a physical DELETE.
/// </summary>
/// <param name="Id">The <c>TB_ACCOUNTEXCEPTION.ID</c> to soft-delete (bound from the route).</param>
public sealed record DeleteAccountExceptionCommand(Guid Id) : IRequest;
