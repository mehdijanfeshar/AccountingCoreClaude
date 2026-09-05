using Accounting.Application.Common.Interfaces;
using MediatR;

namespace Accounting.Application.CheckBooks.Queries.GetCheckBookById;

/// <summary>
/// Delegates straight to <see cref="ICheckBookReadRepository.GetByIdAsync"/>. Read-side
/// handlers never touch <see cref="IUnitOfWork"/> — there is nothing to persist.
/// </summary>
public sealed class GetCheckBookByIdQueryHandler : IRequestHandler<GetCheckBookByIdQuery, CheckBookDto?>
{
    private readonly ICheckBookReadRepository _readRepository;

    public GetCheckBookByIdQueryHandler(ICheckBookReadRepository readRepository)
    {
        _readRepository = readRepository;
    }

    public Task<CheckBookDto?> Handle(GetCheckBookByIdQuery request, CancellationToken cancellationToken)
        => _readRepository.GetByIdAsync(request.Id, cancellationToken);
}
