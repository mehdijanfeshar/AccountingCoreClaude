using Accounting.Application.Common.Exceptions;
using Accounting.Application.Common.Interfaces;
using Accounting.Application.UnitAccess.Queries;
using MediatR;

namespace Accounting.Application.UnitAccess.Queries.GetAccessibleUnits;

/// <summary>
/// Reads the caller's own unit from the token and asks
/// <see cref="IUnitAccessReadRepository"/> for the set it may act as.
/// </summary>
public sealed class GetAccessibleUnitsQueryHandler
    : IRequestHandler<GetAccessibleUnitsQuery, IReadOnlyList<AccessibleUnitDto>>
{
    private readonly ICurrentUser _currentUser;
    private readonly IUnitAccessReadRepository _readRepository;

    public GetAccessibleUnitsQueryHandler(ICurrentUser currentUser, IUnitAccessReadRepository readRepository)
    {
        _currentUser = currentUser;
        _readRepository = readRepository;
    }

    public Task<IReadOnlyList<AccessibleUnitDto>> Handle(
        GetAccessibleUnitsQuery request,
        CancellationToken cancellationToken)
    {
        var vahedCode = _currentUser.VahedCode;

        // Same fail-loud contract as VahedScopeBehavior: a caller whose token carries no usable
        // org claim gets 403, not an empty list. An empty list would read to the client as
        // "you legitimately have access to nothing", hiding a misconfigured token.
        if (string.IsNullOrWhiteSpace(vahedCode))
        {
            throw new MissingVahedScopeException(nameof(GetAccessibleUnitsQuery));
        }

        return GetAsync(vahedCode, cancellationToken);
    }

    private async Task<IReadOnlyList<AccessibleUnitDto>> GetAsync(
        string vahedCode,
        CancellationToken cancellationToken)
    {
        var units = await _readRepository.GetAccessibleUnitsAsync(vahedCode, cancellationToken);

        // A known unit always contains itself, so empty means "this code matched no unit row" —
        // the reference project's UnitCodeNotFoundException case. Reported as a distinct error
        // rather than an empty picker; see UnknownCallerUnitException for why.
        if (units.Count == 0)
        {
            throw new UnknownCallerUnitException(vahedCode);
        }

        return units;
    }
}
