using Accounting.Application.Common.Exceptions;
using Accounting.Application.Common.Interfaces;
using Accounting.Application.FinancialStatements;
using Accounting.Application.FinancialStatements.Commands.Common;
using Accounting.Application.FinancialStatements.Engine;
using Accounting.Domain.ValueObjects;
using FluentValidation;
using FluentValidation.Results;
using MediatR;

namespace Accounting.Application.FinancialStatements.Commands.ActivateFsTemplateVersion;

public sealed class ActivateFsTemplateVersionCommandHandler : IRequestHandler<ActivateFsTemplateVersionCommand>
{
    private readonly IFsTemplateRepository _repository;
    private readonly IFsUnitScopeProvider _scopes;
    private readonly IFsTemplateReadRepository _readRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUser _currentUser;

    public ActivateFsTemplateVersionCommandHandler(
        IFsTemplateRepository repository, IFsUnitScopeProvider scopes,
        IFsTemplateReadRepository readRepository,
        IUnitOfWork unitOfWork,
        ICurrentUser currentUser)
    {
        _repository = repository;
        _scopes = scopes;
        _readRepository = readRepository;
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
    }

    public async Task Handle(ActivateFsTemplateVersionCommand request, CancellationToken cancellationToken)
    {
        var version = await _repository.GetVersionForUpdateAsync(request.Id, cancellationToken)
            ?? throw new NotFoundException("FsTemplateVersion", request.Id);

        FsTemplateRules.EnsureEditable(await _scopes.GetAsync(request.VahedCode, cancellationToken), version.TEMPLATE);

        FsTemplateRules.EnsureDraft(version);

        var rows = await _repository.GetRowsForUpdateAsync(version.ID, cancellationToken);
        var templateCodes = await _readRepository.GetTemplateCodesAsync(cancellationToken);

        var errors = FsTemplateChecker
            .Check(FsTemplateRules.ToCheckRows(rows), version.TEMPLATE.CODE, templateCodes, version.TEMPLATE.NOTE_TOTAL_ROW_CODE)
            .Where(i => i.Severity == FsIssueSeverity.Error)
            .ToList();

        if (errors.Count > 0)
        {
            throw new ValidationException(errors.Select(e =>
                new ValidationFailure(e.RowCode is null ? e.Field : $"{e.RowCode}.{e.Field}", e.Message)));
        }

        var now = DateTime.UtcNow;
        var userId = _currentUser.UserId;

        var siblings = await _repository.GetVersionsForUpdateAsync(version.TEMPLATE_ID, cancellationToken);

        foreach (var other in siblings.Where(v =>
                     v.ID != version.ID
                     && v.STATE == FsTemplateVersionState.Active
                     && v.EFFECTIVE_FROM_YEAR == request.EffectiveFromYear))
        {
            other.STATE = FsTemplateVersionState.Retired;
            other.CHANGEUSERID = userId;
            other.UPDATEDDATE = now;
        }

        version.STATE = FsTemplateVersionState.Active;
        version.EFFECTIVE_FROM_YEAR = request.EffectiveFromYear;
        version.ACTIVATED_BY = userId;
        version.ACTIVATED_DATE = now;
        version.CONTENT_HASH = FsTemplateRules.ComputeContentHash(rows);
        version.CHANGEUSERID = userId;
        version.UPDATEDDATE = now;

        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
