using Accounting.Application.Common.Interfaces;
using Accounting.Application.FinancialStatements.Engine;
using MediatR;

namespace Accounting.Application.FinancialStatements.Queries.ValidateFsTemplateVersion;

public sealed class ValidateFsTemplateVersionQueryHandler : IRequestHandler<ValidateFsTemplateVersionQuery, FsTemplateCheckResultDto?>
{
    private readonly IFsTemplateReadRepository _readRepository;
    private readonly IFsUnitScopeProvider _scopes;

    public ValidateFsTemplateVersionQueryHandler(IFsTemplateReadRepository readRepository, IFsUnitScopeProvider scopes)
    {
        _readRepository = readRepository;
        _scopes = scopes;
    }

    public async Task<FsTemplateCheckResultDto?> Handle(ValidateFsTemplateVersionQuery request, CancellationToken cancellationToken)
    {
        var version = await _readRepository.GetVersionAsync(
            request.Id, await _scopes.GetAsync(request.VahedCode, cancellationToken), cancellationToken);

        if (version is null)
        {
            return null;
        }

        var templateCodes = await _readRepository.GetTemplateCodesAsync(cancellationToken);

        var rows = version.Rows
            .Select(r => new FsCheckRow(
                r.Code, r.ParentCode, r.OrderNo, r.RowType, r.TitleFa, r.NormalBalance, r.Selector, r.ValueType, r.Formula))
            .ToList();

        var issues = FsTemplateChecker.Check(rows, version.TemplateCode, templateCodes, version.NoteTotalRowCode);

        return new FsTemplateCheckResultDto(issues.All(i => i.Severity != FsIssueSeverity.Error), issues);
    }
}
