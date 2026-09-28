using Accounting.Application.Common.Interfaces;
using MediatR;

namespace Accounting.Application.PettyCash.Queries.GetPettyCashFundById;

/// <summary>
/// Delegates straight to <see cref="IPettyCashFundReadRepository.GetByIdAsync"/>. Read-side
/// handlers never touch <see cref="IUnitOfWork"/> — there is nothing to persist.
/// </summary>
public sealed class GetPettyCashFundByIdQueryHandler : IRequestHandler<GetPettyCashFundByIdQuery, PettyCashFundDto?>
{
    private readonly IPettyCashFundReadRepository _readRepository;

    public GetPettyCashFundByIdQueryHandler(IPettyCashFundReadRepository readRepository)
    {
        _readRepository = readRepository;
    }

    public Task<PettyCashFundDto?> Handle(GetPettyCashFundByIdQuery request, CancellationToken cancellationToken)
        => _readRepository.GetByIdAsync(request.Id, request.VahedCode, cancellationToken);
}
