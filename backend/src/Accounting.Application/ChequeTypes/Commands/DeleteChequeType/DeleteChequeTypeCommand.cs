using MediatR;

namespace Accounting.Application.ChequeTypes.Commands.DeleteChequeType;

/// <summary>
/// Soft-deletes a <c>TB_CHECK_TYPE</c> row: sets <c>ISDELETED = true</c> plus the
/// <c>CHANGEUSERID</c>/<c>UPDATEDDATE</c> audit columns. Never issues a physical DELETE — this
/// project never issues physical deletes on any entity (see CLAUDE.md).
/// </summary>
/// <param name="Id">The <c>TB_CHECK_TYPE.ID</c> to soft-delete (bound from the route).</param>
public sealed record DeleteChequeTypeCommand(Guid Id) : IRequest;
