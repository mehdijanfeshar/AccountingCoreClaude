using Accounting.Application.Common;
using Accounting.Application.Common.Interfaces;
using MediatR;

namespace Accounting.Application.CheckBooks.Queries.GetCheckBooks;

/// <summary>
/// Delegates straight to <see cref="ICheckBookReadRepository.GetPagedAsync"/>. Read-side
/// handlers never touch <see cref="IUnitOfWork"/> — there is nothing to persist.
/// </summary>
public sealed class GetCheckBooksQueryHandler : IRequestHandler<GetCheckBooksQuery, PagedResult<CheckBookDto>>
{
    private readonly ICheckBookReadRepository _readRepository;

    public GetCheckBooksQueryHandler(ICheckBookReadRepository readRepository)
    {
        _readRepository = readRepository;
    }

    public Task<PagedResult<CheckBookDto>> Handle(GetCheckBooksQuery request, CancellationToken cancellationToken)
        => _readRepository.GetPagedAsync(request.PageNumber, request.PageSize, cancellationToken);
}
