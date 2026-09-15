using MediatR;

namespace Accounting.Application.PersonActions.Commands.DeletePersonAction;

/// <summary>
/// Soft-deletes a <c>TB_PERSON_ACTION</c> row: sets <c>ISDELETED = true</c> plus the
/// <c>CHANGEUSERID</c>/<c>UPDATEDDATE</c> audit columns. Never issues a physical DELETE.
/// </summary>
/// <param name="Id">The <c>TB_PERSON_ACTION.ID</c> to soft-delete (bound from the route).</param>
public sealed record DeletePersonActionCommand(Guid Id) : IRequest;
