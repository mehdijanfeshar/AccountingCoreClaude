using Accounting.Application.Common;
using Accounting.Application.Common.Interfaces;
using MediatR;

namespace Accounting.Application.AttribForAccountCodes.Queries.GetAttribForAccountCodes;

/// <summary>
/// Delegates straight to <see cref="IAttribForAccountCodeReadRepository.GetPagedAsync"/>.
/// Read-side handlers never touch <see cref="IUnitOfWork"/> — there is nothing to persist.
/// </summary>
public sealed class GetAttribForAccountCodesQueryHandler : IRequestHandler<GetAttribForAccountCodesQuery, PagedResult<AttribForAccountCodeDto>>
{
    private readonly IAttribForAccountCodeReadRepository _readRepository;

    public GetAttribForAccountCodesQueryHandler(IAttribForAccountCodeReadRepository readRepository)
    {
        _readRepository = readRepository;
    }

    public Task<PagedResult<AttribForAccountCodeDto>> Handle(GetAttribForAccountCodesQuery request, CancellationToken cancellationToken)
        => _readRepository.GetPagedAsync(request.PageNumber, request.PageSize, cancellationToken);
}
