using Accounting.Application.Common.Interfaces;
using MediatR;

namespace Accounting.Application.PersonActions.Queries.GetPersonActionById;

/// <summary>
/// Delegates straight to <see cref="IPersonActionReadRepository.GetByIdAsync"/>. Read-side
/// handlers never touch <see cref="IUnitOfWork"/>.
/// </summary>
public sealed class GetPersonActionByIdQueryHandler : IRequestHandler<GetPersonActionByIdQuery, PersonActionDto?>
{
    private readonly IPersonActionReadRepository _readRepository;

    public GetPersonActionByIdQueryHandler(IPersonActionReadRepository readRepository)
    {
        _readRepository = readRepository;
    }

    public Task<PersonActionDto?> Handle(GetPersonActionByIdQuery request, CancellationToken cancellationToken)
        => _readRepository.GetByIdAsync(request.Id, cancellationToken);
}
