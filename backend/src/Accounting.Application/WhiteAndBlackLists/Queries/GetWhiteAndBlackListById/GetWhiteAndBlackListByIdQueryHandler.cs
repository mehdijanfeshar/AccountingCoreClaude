using Accounting.Application.Common.Interfaces;
using MediatR;

namespace Accounting.Application.WhiteAndBlackLists.Queries.GetWhiteAndBlackListById;

/// <summary>
/// Delegates straight to <see cref="IWhiteAndBlackListReadRepository.GetByIdAsync"/>. Read-side
/// handlers never touch <see cref="IUnitOfWork"/> — there is nothing to persist.
/// </summary>
public sealed class GetWhiteAndBlackListByIdQueryHandler : IRequestHandler<GetWhiteAndBlackListByIdQuery, WhiteAndBlackListDto?>
{
    private readonly IWhiteAndBlackListReadRepository _readRepository;

    public GetWhiteAndBlackListByIdQueryHandler(IWhiteAndBlackListReadRepository readRepository)
    {
        _readRepository = readRepository;
    }

    public Task<WhiteAndBlackListDto?> Handle(GetWhiteAndBlackListByIdQuery request, CancellationToken cancellationToken)
        => _readRepository.GetByIdAsync(request.Id, cancellationToken);
}
