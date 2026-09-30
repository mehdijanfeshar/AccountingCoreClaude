using Accounting.Application.Common.Exceptions;
using Accounting.Application.Common.Interfaces;
using Accounting.Application.FinancialStatements;
using Accounting.Application.FinancialStatements.Commands.Common;
using MediatR;

namespace Accounting.Application.FinancialStatements.Commands.ReorderFsTemplateRows;

public sealed class ReorderFsTemplateRowsCommandHandler : IRequestHandler<ReorderFsTemplateRowsCommand>
{
    private readonly IFsTemplateRepository _repository;
    private readonly IFsUnitScopeProvider _scopes;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUser _currentUser;

    public ReorderFsTemplateRowsCommandHandler(IFsTemplateRepository repository, IFsUnitScopeProvider scopes, IUnitOfWork unitOfWork, ICurrentUser currentUser)
    {
        _repository = repository;
        _scopes = scopes;
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
    }

    public async Task Handle(ReorderFsTemplateRowsCommand request, CancellationToken cancellationToken)
    {
        var version = await _repository.GetVersionForUpdateAsync(request.VersionId, cancellationToken)
            ?? throw new NotFoundException("FsTemplateVersion", request.VersionId);

        FsTemplateRules.EnsureEditable(await _scopes.GetAsync(request.VahedCode, cancellationToken), version.TEMPLATE);

        FsTemplateRules.EnsureDraft(version);

        var rows = await _repository.GetRowsForUpdateAsync(version.ID, cancellationToken);
        var byId = rows.ToDictionary(r => r.ID);

        if (request.RowIds.Count != rows.Count || request.RowIds.Any(id => !byId.ContainsKey(id)))
        {
            throw FsTemplateRules.Invalid("RowIds", "فهرست ترتیب باید دقیقاً همهٔ ردیف‌های این نسخه را داشته باشد.");
        }

        var now = DateTime.UtcNow;

        for (var i = 0; i < request.RowIds.Count; i++)
        {
            var row = byId[request.RowIds[i]];
            var orderNo = (i + 1) * 10;

            if (row.ORDER_NO != orderNo)
            {
                row.ORDER_NO = orderNo;
                row.CHANGEUSERID = _currentUser.UserId;
                row.UPDATEDDATE = now;
            }
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
