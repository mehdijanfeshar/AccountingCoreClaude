using Accounting.Application.Common.Interfaces;
using MediatR;

namespace Accounting.Application.FinancialStatements.Queries.GetFsTemplates;

public sealed class GetFsTemplatesQueryHandler : IRequestHandler<GetFsTemplatesQuery, IReadOnlyList<FsTemplateDto>>
{
    private readonly IFsTemplateReadRepository _readRepository;
    private readonly IFsUnitScopeProvider _scopes;

    public GetFsTemplatesQueryHandler(IFsTemplateReadRepository readRepository, IFsUnitScopeProvider scopes)
    {
        _readRepository = readRepository;
        _scopes = scopes;
    }

    public async Task<IReadOnlyList<FsTemplateDto>> Handle(GetFsTemplatesQuery request, CancellationToken cancellationToken)
        => await _readRepository.GetTemplatesAsync(
            request.Framework, await _scopes.GetAsync(request.VahedCode, cancellationToken), cancellationToken);
}
