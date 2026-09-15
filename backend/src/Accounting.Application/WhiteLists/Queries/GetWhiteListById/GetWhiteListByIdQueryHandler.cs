using Accounting.Application.Common.Interfaces;
using MediatR;

namespace Accounting.Application.WhiteLists.Queries.GetWhiteListById;

/// <summary>
/// Delegates straight to <see cref="IWhiteListReadRepository.GetByIdAsync"/>. Read-side
/// handlers never touch <see cref="IUnitOfWork"/> — there is nothing to persist.
/// </summary>
public sealed class GetWhiteListByIdQueryHandler : IRequestHandler<GetWhiteListByIdQuery, WhiteListDto?>
{
    private readonly IWhiteListReadRepository _readRepository;

    public GetWhiteListByIdQueryHandler(IWhiteListReadRepository readRepository)
    {
        _readRepository = readRepository;
    }

    public Task<WhiteListDto?> Handle(GetWhiteListByIdQuery request, CancellationToken cancellationToken)
        => _readRepository.GetByIdAsync(request.Id, cancellationToken);
}
