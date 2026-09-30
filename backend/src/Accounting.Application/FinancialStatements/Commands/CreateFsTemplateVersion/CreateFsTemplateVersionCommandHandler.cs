using Accounting.Application.Common.Exceptions;
using Accounting.Application.Common.Interfaces;
using Accounting.Application.FinancialStatements.Commands.Common;
using Accounting.Domain.Entity;
using Accounting.Domain.ValueObjects;
using MediatR;

namespace Accounting.Application.FinancialStatements.Commands.CreateFsTemplateVersion;

public sealed class CreateFsTemplateVersionCommandHandler : IRequestHandler<CreateFsTemplateVersionCommand, Guid>
{
    private readonly IFsTemplateRepository _repository;
    private readonly IFsUnitScopeProvider _scopes;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUser _currentUser;

    public CreateFsTemplateVersionCommandHandler(IFsTemplateRepository repository, IFsUnitScopeProvider scopes, IUnitOfWork unitOfWork, ICurrentUser currentUser)
    {
        _repository = repository;
        _scopes = scopes;
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
    }

    public async Task<Guid> Handle(CreateFsTemplateVersionCommand request, CancellationToken cancellationToken)
    {
        var template = await _repository.GetTemplateForUpdateAsync(request.TemplateId, cancellationToken)
            ?? throw new NotFoundException("FsTemplate", request.TemplateId);

        FsTemplateRules.EnsureEditable(await _scopes.GetAsync(request.VahedCode, cancellationToken), template);

        var versions = await _repository.GetVersionsForUpdateAsync(template.ID, cancellationToken);

        if (versions.FirstOrDefault(v => v.STATE == FsTemplateVersionState.Draft) is { } openDraft)
        {
            throw new FsTemplateConflictException(
                $"این قالب یک نسخهٔ پیش‌نویس باز دارد (نسخهٔ {openDraft.VERSION_NO}). همان را ویرایش یا حذف کنید.");
        }

        TB_FS_TEMPLATE_VERSION? source;

        if (request.SourceVersionId is { } sourceId)
        {
            source = versions.FirstOrDefault(v => v.ID == sourceId)
                ?? throw new NotFoundException("FsTemplateVersion", sourceId);
        }
        else
        {
            source = versions.OrderByDescending(v => v.VERSION_NO).FirstOrDefault();
        }

        var now = DateTime.UtcNow;
        var userId = _currentUser.UserId;

        var version = new TB_FS_TEMPLATE_VERSION
        {
            ID = Guid.NewGuid(),
            TEMPLATE_ID = template.ID,
            VERSION_NO = await _repository.GetMaxVersionNoAsync(template.ID, cancellationToken) + 1,
            STATE = FsTemplateVersionState.Draft,
            DESCRIPTION = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim(),
            CREATEDDATE = now,
            ADDUSERID = userId,
        };

        await _repository.AddVersionAsync(version, cancellationToken);

        if (source is not null)
        {
            var sourceRows = await _repository.GetRowsForUpdateAsync(source.ID, cancellationToken);
            var idMap = sourceRows.ToDictionary(r => r.ID, _ => Guid.NewGuid());

            await _repository.AddRowsAsync(
                sourceRows.Select(r => new TB_FS_TEMPLATE_ROW
                {
                    ID = idMap[r.ID],
                    VERSION_ID = version.ID,
                    CODE = r.CODE,
                    PARENT_ID = r.PARENT_ID is { } pid && idMap.TryGetValue(pid, out var newPid) ? newPid : null,
                    ORDER_NO = r.ORDER_NO,
                    ROW_TYPE = r.ROW_TYPE,
                    TITLE_FA = r.TITLE_FA,
                    TITLE_EN = r.TITLE_EN,
                    NOTE_REF = r.NOTE_REF,
                    NORMAL_BALANCE = r.NORMAL_BALANCE,
                    SELECTOR = r.SELECTOR,
                    VALUE_TYPE = r.VALUE_TYPE,
                    FORMULA = r.FORMULA,
                    FORMAT_JSON = r.FORMAT_JSON,
                    IS_DRILLABLE = r.IS_DRILLABLE,
                    ALLOW_MANUAL_ADJUST = r.ALLOW_MANUAL_ADJUST,
                    CREATEDDATE = now,
                    ADDUSERID = userId,
                }).ToList(),
                cancellationToken);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return version.ID;
    }
}
