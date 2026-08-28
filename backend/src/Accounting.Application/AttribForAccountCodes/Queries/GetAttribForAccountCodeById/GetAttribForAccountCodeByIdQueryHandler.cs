using Accounting.Application.Common.Interfaces;
using MediatR;

namespace Accounting.Application.AttribForAccountCodes.Queries.GetAttribForAccountCodeById;

/// <summary>
/// Delegates straight to <see cref="IAttribForAccountCodeReadRepository.GetByIdAsync"/>.
/// Read-side handlers never touch <see cref="IUnitOfWork"/> — there is nothing to persist.
/// </summary>
public sealed class GetAttribForAccountCodeByIdQueryHandler : IRequestHandler<GetAttribForAccountCodeByIdQuery, AttribForAccountCodeDto?>
{
    private readonly IAttribForAccountCodeReadRepository _readRepository;

    public GetAttribForAccountCodeByIdQueryHandler(IAttribForAccountCodeReadRepository readRepository)
    {
        _readRepository = readRepository;
    }

    public Task<AttribForAccountCodeDto?> Handle(GetAttribForAccountCodeByIdQuery request, CancellationToken cancellationToken)
        => _readRepository.GetByIdAsync(request.Id, cancellationToken);
}
