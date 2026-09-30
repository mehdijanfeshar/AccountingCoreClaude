using Accounting.Application.Common.Exceptions;
using Accounting.Application.Common.Interfaces;
using Accounting.Application.FinancialStatements.Commands.Common;
using Accounting.Domain.ValueObjects;
using MediatR;

namespace Accounting.Application.FinancialStatements.Commands.DeleteFsTemplate;

public sealed class DeleteFsTemplateCommandHandler : IRequestHandler<DeleteFsTemplateCommand>
{
    private readonly IFsTemplateRepository _repository;
    private readonly IFsUnitScopeProvider _scopes;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUser _currentUser;

    public DeleteFsTemplateCommandHandler(IFsTemplateRepository repository, IFsUnitScopeProvider scopes, IUnitOfWork unitOfWork, ICurrentUser currentUser)
    {
        _repository = repository;
        _scopes = scopes;
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
    }

    public async Task Handle(DeleteFsTemplateCommand request, CancellationToken cancellationToken)
    {
        var template = await _repository.GetTemplateForUpdateAsync(request.Id, cancellationToken)
            ?? throw new NotFoundException("FsTemplate", request.Id);

        FsTemplateRules.EnsureEditable(await _scopes.GetAsync(request.VahedCode, cancellationToken), template);

        var versions = await _repository.GetVersionsForUpdateAsync(template.ID, cancellationToken);

        if (versions.Any(v => v.STATE == FsTemplateVersionState.Active))
        {
            throw new FsTemplateConflictException("این قالب نسخهٔ فعال دارد و قابل حذف نیست.");
        }

        var now = DateTime.UtcNow;
        var userId = _currentUser.UserId;

        foreach (var v in versions)
        {
            v.ISDELETED = true;
            v.CHANGEUSERID = userId;
            v.UPDATEDDATE = now;
        }

        template.ISDELETED = true;
        template.CHANGEUSERID = userId;
        template.UPDATEDDATE = now;

        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
