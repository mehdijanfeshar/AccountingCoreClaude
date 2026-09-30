using Accounting.Application.Common.Exceptions;
using Accounting.Application.Common.Interfaces;
using Accounting.Application.FinancialStatements;
using Accounting.Application.FinancialStatements.Commands.Common;
using MediatR;

namespace Accounting.Application.FinancialStatements.Commands.UpdateFsTemplateVersion;

public sealed class UpdateFsTemplateVersionCommandHandler : IRequestHandler<UpdateFsTemplateVersionCommand>
{
    private readonly IFsTemplateRepository _repository;
    private readonly IFsUnitScopeProvider _scopes;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUser _currentUser;

    public UpdateFsTemplateVersionCommandHandler(IFsTemplateRepository repository, IFsUnitScopeProvider scopes, IUnitOfWork unitOfWork, ICurrentUser currentUser)
    {
        _repository = repository;
        _scopes = scopes;
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
    }

    public async Task Handle(UpdateFsTemplateVersionCommand request, CancellationToken cancellationToken)
    {
        var version = await _repository.GetVersionForUpdateAsync(request.Id, cancellationToken)
            ?? throw new NotFoundException("FsTemplateVersion", request.Id);

        FsTemplateRules.EnsureEditable(await _scopes.GetAsync(request.VahedCode, cancellationToken), version.TEMPLATE);

        FsTemplateRules.EnsureDraft(version);

        version.DESCRIPTION = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim();
        version.CHANGEUSERID = _currentUser.UserId;
        version.UPDATEDDATE = DateTime.UtcNow;

        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
