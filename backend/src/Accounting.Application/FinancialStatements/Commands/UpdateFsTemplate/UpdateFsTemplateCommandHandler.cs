using Accounting.Application.Common.Exceptions;
using Accounting.Application.Common.Interfaces;
using Accounting.Application.FinancialStatements.Commands.Common;
using MediatR;

namespace Accounting.Application.FinancialStatements.Commands.UpdateFsTemplate;

public sealed class UpdateFsTemplateCommandHandler : IRequestHandler<UpdateFsTemplateCommand>
{
    private readonly IFsTemplateRepository _repository;
    private readonly IFsUnitScopeProvider _scopes;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUser _currentUser;

    public UpdateFsTemplateCommandHandler(IFsTemplateRepository repository, IFsUnitScopeProvider scopes, IUnitOfWork unitOfWork, ICurrentUser currentUser)
    {
        _repository = repository;
        _scopes = scopes;
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
    }

    public async Task Handle(UpdateFsTemplateCommand request, CancellationToken cancellationToken)
    {
        var template = await _repository.GetTemplateForUpdateAsync(request.Id, cancellationToken)
            ?? throw new NotFoundException("FsTemplate", request.Id);

        FsTemplateRules.EnsureEditable(await _scopes.GetAsync(request.VahedCode, cancellationToken), template);

        template.TITLE_FA = request.TitleFa.Trim();
        template.TITLE_EN = string.IsNullOrWhiteSpace(request.TitleEn) ? null : request.TitleEn.Trim();
        template.ORDER_NO = request.OrderNo;
        FsTemplateRules.ApplyNoteLink(template, request.NoteParentTemplateCode, request.NoteParentRowCode, request.NoteTotalRowCode);
        template.CHANGEUSERID = _currentUser.UserId;
        template.UPDATEDDATE = DateTime.UtcNow;

        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
