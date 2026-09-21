using Accounting.Application.Common.Interfaces;
using Accounting.Application.IdentityHeads.Queries;
using MediatR;

namespace Accounting.Application.IdentityHeads.Queries.GetIdentityHeadById;

public sealed class GetIdentityHeadByIdQueryHandler : IRequestHandler<GetIdentityHeadByIdQuery, IdentityHeadDto?>
{
    private readonly IIdentityHeadReadRepository _readRepository;

    public GetIdentityHeadByIdQueryHandler(IIdentityHeadReadRepository readRepository)
    {
        _readRepository = readRepository;
    }

    public Task<IdentityHeadDto?> Handle(GetIdentityHeadByIdQuery request, CancellationToken cancellationToken)
        => _readRepository.GetByIdAsync(request.Id, request.VahedCode, cancellationToken);
}
