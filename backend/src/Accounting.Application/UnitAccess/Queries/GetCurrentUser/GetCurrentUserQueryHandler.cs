using Accounting.Application.Common.Interfaces;
using Accounting.Application.UnitAccess.Queries;
using MediatR;

namespace Accounting.Application.UnitAccess.Queries.GetCurrentUser;

/// <summary>
/// Combines the token-derived identity (<see cref="ICurrentUser"/>) with the unit row that the
/// token's org claim names.
/// </summary>
public sealed class GetCurrentUserQueryHandler : IRequestHandler<GetCurrentUserQuery, CurrentUserDto>
{
    private readonly ICurrentUser _currentUser;
    private readonly IUnitAccessReadRepository _readRepository;

    public GetCurrentUserQueryHandler(ICurrentUser currentUser, IUnitAccessReadRepository readRepository)
    {
        _currentUser = currentUser;
        _readRepository = readRepository;
    }

    public async Task<CurrentUserDto> Handle(GetCurrentUserQuery request, CancellationToken cancellationToken)
    {
        var vahedCode = _currentUser.VahedCode;

        // Unlike GetAccessibleUnitsQuery this does NOT throw on a missing org claim. This endpoint
        // is what a client calls to find out what state it is in; answering "you are authenticated
        // but your token names no unit" is strictly more useful than a 403 that looks identical to
        // a permissions problem.
        if (string.IsNullOrWhiteSpace(vahedCode))
        {
            return new CurrentUserDto(_currentUser.UserId, VahedCode: null, VahedName: null, IsHeadquarters: false);
        }

        var profile = await _readRepository.GetUnitProfileAsync(vahedCode, cancellationToken);

        return new CurrentUserDto(
            _currentUser.UserId,
            vahedCode,
            profile?.VahedName,
            profile?.IsHeadquarters ?? false);
    }
}
