using Accounting.Application.Common;
using Accounting.Application.Common.Interfaces;
using MediatR;

namespace Accounting.Application.AttribForAccountCodes.Queries.GetAttribForAccountCodes;

/// <summary>
/// Delegates straight to <see cref="IAttribForAccountCodeReadRepository.GetPagedAsync"/>.
/// Read-side handlers never touch <see cref="IUnitOfWork"/> — there is nothing to persist.
///
/// <c>request.VahedCode</c> is passed through as-is, at face value: by the time this handler
/// runs, <c>VahedScopeBehavior</c> has already overwritten it with the authenticated caller's own
/// unit code, so this handler never reads <see cref="ICurrentUser"/> directly.
/// </summary>
public sealed class GetAttribForAccountCodesQueryHandler : IRequestHandler<GetAttribForAccountCodesQuery, PagedResult<AttribForAccountCodeDto>>
{
    private readonly IAttribForAccountCodeReadRepository _readRepository;

    public GetAttribForAccountCodesQueryHandler(IAttribForAccountCodeReadRepository readRepository)
    {
        _readRepository = readRepository;
    }

    public Task<PagedResult<AttribForAccountCodeDto>> Handle(GetAttribForAccountCodesQuery request, CancellationToken cancellationToken)
        => _readRepository.GetPagedAsync(request.PageNumber, request.PageSize, request.VahedCode, cancellationToken);
}
