using Accounting.Application.Common.Exceptions;
using Accounting.Application.Common.Interfaces;
using Accounting.Application.FinancialStatements;
using Accounting.Application.FinancialStatements.Commands.Common;
using MediatR;

namespace Accounting.Application.FinancialStatements.Commands.DeleteFsTemplateVersion;

public sealed class DeleteFsTemplateVersionCommandHandler : IRequestHandler<DeleteFsTemplateVersionCommand>
{
    private readonly IFsTemplateRepository _repository;
    private readonly IFsUnitScopeProvider _scopes;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUser _currentUser;

    public DeleteFsTemplateVersionCommandHandler(IFsTemplateRepository repository, IFsUnitScopeProvider scopes, IUnitOfWork unitOfWork, ICurrentUser currentUser)
    {
        _repository = repository;
        _scopes = scopes;
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
    }

    public async Task Handle(DeleteFsTemplateVersionCommand request, CancellationToken cancellationToken)
    {
        var version = await _repository.GetVersionForUpdateAsync(request.Id, cancellationToken)
            ?? throw new NotFoundException("FsTemplateVersion", request.Id);

        FsTemplateRules.EnsureEditable(await _scopes.GetAsync(request.VahedCode, cancellationToken), version.TEMPLATE);

        FsTemplateRules.EnsureDraft(version);

        version.ISDELETED = true;
        version.CHANGEUSERID = _currentUser.UserId;
        version.UPDATEDDATE = DateTime.UtcNow;

        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
