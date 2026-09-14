using Accounting.Application.Common.Interfaces;
using MediatR;

namespace Accounting.Application.SysTypes.Queries.GetSysTypes;

/// <summary>
/// Delegates straight to <see cref="ISysTypeReadRepository.GetAllAsync"/>. Read-side handlers
/// never touch <c>IUnitOfWork</c> — there is nothing to persist.
/// </summary>
public sealed class GetSysTypesQueryHandler : IRequestHandler<GetSysTypesQuery, IReadOnlyList<SysTypeDto>>
{
    private readonly ISysTypeReadRepository _readRepository;

    public GetSysTypesQueryHandler(ISysTypeReadRepository readRepository)
    {
        _readRepository = readRepository;
    }

    public Task<IReadOnlyList<SysTypeDto>> Handle(GetSysTypesQuery request, CancellationToken cancellationToken)
        => _readRepository.GetAllAsync(cancellationToken);
}
