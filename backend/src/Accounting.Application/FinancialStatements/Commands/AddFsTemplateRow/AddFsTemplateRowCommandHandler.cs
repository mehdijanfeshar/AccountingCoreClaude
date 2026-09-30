using Accounting.Application.Common.Exceptions;
using Accounting.Application.Common.Interfaces;
using Accounting.Application.FinancialStatements;
using Accounting.Application.FinancialStatements.Commands.Common;
using Accounting.Domain.Entity;
using MediatR;

namespace Accounting.Application.FinancialStatements.Commands.AddFsTemplateRow;

public sealed class AddFsTemplateRowCommandHandler : IRequestHandler<AddFsTemplateRowCommand, Guid>
{
    private readonly IFsTemplateRepository _repository;
    private readonly IFsUnitScopeProvider _scopes;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUser _currentUser;

    public AddFsTemplateRowCommandHandler(IFsTemplateRepository repository, IFsUnitScopeProvider scopes, IUnitOfWork unitOfWork, ICurrentUser currentUser)
    {
        _repository = repository;
        _scopes = scopes;
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
    }

    public async Task<Guid> Handle(AddFsTemplateRowCommand request, CancellationToken cancellationToken)
    {
        var version = await _repository.GetVersionForUpdateAsync(request.VersionId, cancellationToken)
            ?? throw new NotFoundException("FsTemplateVersion", request.VersionId);

        FsTemplateRules.EnsureEditable(await _scopes.GetAsync(request.VahedCode, cancellationToken), version.TEMPLATE);

        FsTemplateRules.EnsureDraft(version);

        var rows = await _repository.GetRowsForUpdateAsync(version.ID, cancellationToken);

        if (rows.Any(r => r.CODE == request.Row.Code))
        {
            throw new FsTemplateConflictException($"ردیفی با کد «{request.Row.Code}» در این نسخه وجود دارد.");
        }

        var row = new TB_FS_TEMPLATE_ROW
        {
            ID = Guid.NewGuid(),
            VERSION_ID = version.ID,
            ORDER_NO = request.Row.OrderNo ?? (rows.Count == 0 ? 10 : rows.Max(r => r.ORDER_NO) + 10),
            CREATEDDATE = DateTime.UtcNow,
            ADDUSERID = _currentUser.UserId,
        };

        FsTemplateRules.ApplyInput(row, request.Row);
        row.PARENT_ID = FsTemplateRules.ResolveParent(request.Row.ParentCode, row, rows);

        await _repository.AddRowsAsync(new[] { row }, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return row.ID;
    }
}
