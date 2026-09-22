using Accounting.Application.UnitAccess.Queries;
using MediatR;

namespace Accounting.Application.UnitAccess.Queries.GetCurrentUser;

/// <summary>
/// Describes the authenticated caller. Parameterless for the same reason as
/// <c>GetAccessibleUnitsQuery</c>: the subject is always the token's owner, never a client
/// argument.
/// </summary>
public sealed record GetCurrentUserQuery : IRequest<CurrentUserDto>;
