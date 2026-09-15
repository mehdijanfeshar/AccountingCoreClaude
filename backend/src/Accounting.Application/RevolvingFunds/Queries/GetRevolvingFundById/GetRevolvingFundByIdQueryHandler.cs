using Accounting.Application.Common.Interfaces;
using MediatR;

namespace Accounting.Application.RevolvingFunds.Queries.GetRevolvingFundById;

/// <summary>
/// Delegates straight to <see cref="IRevolvingFundReadRepository.GetByIdAsync"/>. Read-side
/// handlers never touch <see cref="IUnitOfWork"/> — there is nothing to persist.
/// </summary>
public sealed class GetRevolvingFundByIdQueryHandler : IRequestHandler<GetRevolvingFundByIdQuery, RevolvingFundDto?>
{
    private readonly IRevolvingFundReadRepository _readRepository;

    public GetRevolvingFundByIdQueryHandler(IRevolvingFundReadRepository readRepository)
    {
        _readRepository = readRepository;
    }

    public Task<RevolvingFundDto?> Handle(GetRevolvingFundByIdQuery request, CancellationToken cancellationToken)
        => _readRepository.GetByIdAsync(request.Id, cancellationToken);
}
