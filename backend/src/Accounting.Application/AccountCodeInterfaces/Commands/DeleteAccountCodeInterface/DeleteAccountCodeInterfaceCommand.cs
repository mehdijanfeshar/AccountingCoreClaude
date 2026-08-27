using MediatR;

namespace Accounting.Application.AccountCodeInterfaces.Commands.DeleteAccountCodeInterface;

/// <summary>
/// Soft-deletes a <c>TB_ACCOUNTCODE_INTERFACE</c> row: sets <c>ISDELETED = true</c> plus the
/// <c>CHANGEUSERID</c>/<c>UPDATEDDATE</c> audit columns. Never issues a physical DELETE.
/// </summary>
/// <param name="Id">The <c>TB_ACCOUNTCODE_INTERFACE.ID</c> to soft-delete (bound from the route).</param>
public sealed record DeleteAccountCodeInterfaceCommand(Guid Id) : IRequest;
