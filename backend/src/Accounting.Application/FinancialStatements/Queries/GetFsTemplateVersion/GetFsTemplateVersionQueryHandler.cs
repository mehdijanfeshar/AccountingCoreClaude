using Accounting.Application.Common.Interfaces;
using MediatR;

namespace Accounting.Application.FinancialStatements.Queries.GetFsTemplateVersion;

public sealed class GetFsTemplateVersionQueryHandler : IRequestHandler<GetFsTemplateVersionQuery, FsTemplateVersionDetailDto?>
{
    private readonly IFsTemplateReadRepository _readRepository;
    private readonly IFsUnitScopeProvider _scopes;

    public GetFsTemplateVersionQueryHandler(IFsTemplateReadRepository readRepository, IFsUnitScopeProvider scopes)
    {
        _readRepository = readRepository;
        _scopes = scopes;
    }

    public async Task<FsTemplateVersionDetailDto?> Handle(GetFsTemplateVersionQuery request, CancellationToken cancellationToken)
        => await _readRepository.GetVersionAsync(
            request.Id, await _scopes.GetAsync(request.VahedCode, cancellationToken), cancellationToken);
}
