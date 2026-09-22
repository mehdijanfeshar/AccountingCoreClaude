using Accounting.Application.Common.Interfaces;
using Accounting.Application.Years.Queries;
using MediatR;

namespace Accounting.Application.Years.Queries.GetYears;

/// <summary>
/// Delegates straight to <see cref="IYearReadRepository.GetAllAsync"/>. Read-side handlers never
/// touch <see cref="IUnitOfWork"/> — there is nothing to persist.
/// </summary>
public sealed class GetYearsQueryHandler : IRequestHandler<GetYearsQuery, IReadOnlyList<YearDto>>
{
    private readonly IYearReadRepository _readRepository;

    public GetYearsQueryHandler(IYearReadRepository readRepository)
    {
        _readRepository = readRepository;
    }

    public Task<IReadOnlyList<YearDto>> Handle(GetYearsQuery request, CancellationToken cancellationToken)
        => _readRepository.GetAllAsync(cancellationToken);
}
