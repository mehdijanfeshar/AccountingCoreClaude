using Accounting.Application.Common.Exceptions;
using Accounting.Application.Common.Interfaces;
using Accounting.Application.FinancialStatements;
using Accounting.Application.FinancialStatements.Commands.Common;
using Accounting.Domain.ValueObjects;
using MediatR;

namespace Accounting.Application.FinancialStatements.Commands.UpdateFsTemplateRow;

public sealed class UpdateFsTemplateRowCommandHandler : IRequestHandler<UpdateFsTemplateRowCommand>
{
    private readonly IFsTemplateRepository _repository;
    private readonly IFsUnitScopeProvider _scopes;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUser _currentUser;

    public UpdateFsTemplateRowCommandHandler(IFsTemplateRepository repository, IFsUnitScopeProvider scopes, IUnitOfWork unitOfWork, ICurrentUser currentUser)
    {
        _repository = repository;
        _scopes = scopes;
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
    }

    public async Task Handle(UpdateFsTemplateRowCommand request, CancellationToken cancellationToken)
    {
        var version = await _repository.GetVersionForUpdateAsync(request.VersionId, cancellationToken)
            ?? throw new NotFoundException("FsTemplateVersion", request.VersionId);

        FsTemplateRules.EnsureEditable(await _scopes.GetAsync(request.VahedCode, cancellationToken), version.TEMPLATE);

        FsTemplateRules.EnsureDraft(version);

        var rows = await _repository.GetRowsForUpdateAsync(version.ID, cancellationToken);
        var row = rows.FirstOrDefault(r => r.ID == request.RowId)
            ?? throw new NotFoundException("FsTemplateRow", request.RowId);

        if (rows.Any(r => r.ID != row.ID && r.CODE == request.Row.Code))
        {
            throw new FsTemplateConflictException($"ردیفی با کد «{request.Row.Code}» در این نسخه وجود دارد.");
        }

        if (row.ROW_TYPE == FsRowType.Header
            && request.Row.RowType != FsRowType.Header
            && rows.Any(r => r.PARENT_ID == row.ID))
        {
            throw new FsTemplateConflictException(
                $"ردیف «{row.CODE}» عنوان گروه ردیف‌های دیگر است؛ پیش از تغییر نوع، فرزندانش را جابه‌جا کنید.");
        }

        FsTemplateRules.ApplyInput(row, request.Row);
        row.PARENT_ID = FsTemplateRules.ResolveParent(request.Row.ParentCode, row, rows);

        if (request.Row.OrderNo is { } orderNo)
        {
            row.ORDER_NO = orderNo;
        }

        row.CHANGEUSERID = _currentUser.UserId;
        row.UPDATEDDATE = DateTime.UtcNow;

        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
