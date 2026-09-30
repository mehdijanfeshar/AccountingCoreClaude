using Accounting.Application.Common.Exceptions;
using Accounting.Application.Common.Interfaces;
using Accounting.Application.FinancialStatements;
using Accounting.Application.FinancialStatements.Commands.Common;
using MediatR;

namespace Accounting.Application.FinancialStatements.Commands.DeleteFsTemplateRow;

public sealed class DeleteFsTemplateRowCommandHandler : IRequestHandler<DeleteFsTemplateRowCommand>
{
    private readonly IFsTemplateRepository _repository;
    private readonly IFsUnitScopeProvider _scopes;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUser _currentUser;

    public DeleteFsTemplateRowCommandHandler(IFsTemplateRepository repository, IFsUnitScopeProvider scopes, IUnitOfWork unitOfWork, ICurrentUser currentUser)
    {
        _repository = repository;
        _scopes = scopes;
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
    }

    public async Task Handle(DeleteFsTemplateRowCommand request, CancellationToken cancellationToken)
    {
        var version = await _repository.GetVersionForUpdateAsync(request.VersionId, cancellationToken)
            ?? throw new NotFoundException("FsTemplateVersion", request.VersionId);

        FsTemplateRules.EnsureEditable(await _scopes.GetAsync(request.VahedCode, cancellationToken), version.TEMPLATE);

        FsTemplateRules.EnsureDraft(version);

        var rows = await _repository.GetRowsForUpdateAsync(version.ID, cancellationToken);
        var row = rows.FirstOrDefault(r => r.ID == request.RowId)
            ?? throw new NotFoundException("FsTemplateRow", request.RowId);

        var now = DateTime.UtcNow;

        foreach (var child in rows.Where(r => r.PARENT_ID == row.ID))
        {
            child.PARENT_ID = null;
            child.CHANGEUSERID = _currentUser.UserId;
            child.UPDATEDDATE = now;
        }

        _repository.RemoveRows(new[] { row });
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
