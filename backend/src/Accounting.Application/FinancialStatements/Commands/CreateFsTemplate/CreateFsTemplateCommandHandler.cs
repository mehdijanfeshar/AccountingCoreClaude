using Accounting.Application.Common.Exceptions;
using Accounting.Application.Common.Interfaces;
using Accounting.Application.FinancialStatements;
using Accounting.Domain.Entity;
using Accounting.Domain.ValueObjects;
using MediatR;

namespace Accounting.Application.FinancialStatements.Commands.CreateFsTemplate;

public sealed class CreateFsTemplateCommandHandler : IRequestHandler<CreateFsTemplateCommand, CreateFsTemplateResult>
{
    private readonly IFsTemplateRepository _repository;
    private readonly IFsUnitScopeProvider _scopes;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUser _currentUser;

    public CreateFsTemplateCommandHandler(IFsTemplateRepository repository, IFsUnitScopeProvider scopes, IUnitOfWork unitOfWork, ICurrentUser currentUser)
    {
        _repository = repository;
        _scopes = scopes;
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
    }

    public async Task<CreateFsTemplateResult> Handle(CreateFsTemplateCommand request, CancellationToken cancellationToken)
    {
        var owner = request.Shared ? null : request.VahedCode;
        (await _scopes.GetAsync(request.VahedCode, cancellationToken)).EnsureCanEdit(owner);

        if (await _repository.TemplateCodeExistsAsync(owner, request.Code, cancellationToken))
        {
            throw new FsTemplateConflictException(owner is null
                ? $"قالب مشترکی با کد «{request.Code}» قبلاً ساخته شده است."
                : $"این واحد قبلاً قالبی با کد «{request.Code}» ساخته است.");
        }

        var now = DateTime.UtcNow;
        var userId = _currentUser.UserId;

        var template = new TB_FS_TEMPLATE
        {
            ID = Guid.NewGuid(),
            VAHEDCODE = owner,
            FRAMEWORK = request.Framework,
            CODE = request.Code,
            TITLE_FA = request.TitleFa.Trim(),
            TITLE_EN = string.IsNullOrWhiteSpace(request.TitleEn) ? null : request.TitleEn.Trim(),
            STATEMENT_TYPE = request.StatementType,
            ORDER_NO = request.OrderNo,
            CREATEDDATE = now,
            ADDUSERID = userId,
        };

        Common.FsTemplateRules.ApplyNoteLink(template, request.NoteParentTemplateCode, request.NoteParentRowCode, request.NoteTotalRowCode);

        var version = new TB_FS_TEMPLATE_VERSION
        {
            ID = Guid.NewGuid(),
            TEMPLATE_ID = template.ID,
            VERSION_NO = 1,
            STATE = FsTemplateVersionState.Draft,
            CREATEDDATE = now,
            ADDUSERID = userId,
        };

        await _repository.AddTemplateAsync(template, cancellationToken);
        await _repository.AddVersionAsync(version, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new CreateFsTemplateResult(template.ID, version.ID);
    }
}
