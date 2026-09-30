using Accounting.Application.Common.Exceptions;
using Accounting.Application.Common.Interfaces;
using Accounting.Application.FinancialStatements;
using Accounting.Application.FinancialStatements.Commands.Common;
using MediatR;

namespace Accounting.Application.FinancialStatements.Commands.ImportFsTemplateRows;

public sealed class ImportFsTemplateRowsCommandHandler : IRequestHandler<ImportFsTemplateRowsCommand, int>
{
    private readonly IFsTemplateRepository _repository;
    private readonly IFsUnitScopeProvider _scopes;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUser _currentUser;

    public ImportFsTemplateRowsCommandHandler(IFsTemplateRepository repository, IFsUnitScopeProvider scopes, IUnitOfWork unitOfWork, ICurrentUser currentUser)
    {
        _repository = repository;
        _scopes = scopes;
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
    }

    public async Task<int> Handle(ImportFsTemplateRowsCommand request, CancellationToken cancellationToken)
    {
        var version = await _repository.GetVersionForUpdateAsync(request.VersionId, cancellationToken)
            ?? throw new NotFoundException("FsTemplateVersion", request.VersionId);

        FsTemplateRules.EnsureEditable(await _scopes.GetAsync(request.VahedCode, cancellationToken), version.TEMPLATE);

        FsTemplateRules.EnsureDraft(version);

        // پیش از هر تغییری ساخته می‌شود تا خطای والد/کد تکراری چیزی را نیمه‌کاره نگذارد.
        var newRows = FsTemplateRules.BuildRows(version.ID, request.Rows, _currentUser.UserId, DateTime.UtcNow);
        var oldRows = await _repository.GetRowsForUpdateAsync(version.ID, cancellationToken);

        // دو SaveChanges در یک تراکنش: حذف ردیف‌های قبلی باید پیش از درج ردیف‌های هم‌کد انجام شود
        // (UK_FS_TEMPLATE_ROW_CODE روی VERSION_ID, CODE).
        await _unitOfWork.BeginTransactionAsync(cancellationToken);

        try
        {
            if (oldRows.Count > 0)
            {
                _repository.RemoveRows(oldRows);
                await _unitOfWork.SaveChangesAsync(cancellationToken);
            }

            await _repository.AddRowsAsync(newRows, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            await _unitOfWork.CommitTransactionAsync(cancellationToken);
        }
        catch
        {
            await _unitOfWork.RollbackTransactionAsync(cancellationToken);
            throw;
        }

        return newRows.Count;
    }
}
