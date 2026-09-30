using Accounting.Application.Common.Interfaces;
using Accounting.Application.FinancialStatements;
using Accounting.Application.FinancialStatements.Commands.Common;
using Accounting.Domain.Entity;
using Accounting.Domain.ValueObjects;
using MediatR;

namespace Accounting.Application.FinancialStatements.Commands.SeedDefaultFsTemplates;

public sealed class SeedDefaultFsTemplatesCommandHandler : IRequestHandler<SeedDefaultFsTemplatesCommand, IReadOnlyList<string>>
{
    private readonly IFsTemplateRepository _repository;
    private readonly IFsUnitScopeProvider _scopes;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUser _currentUser;
    private readonly IFsCheckRuleRepository _ruleRepository;

    public SeedDefaultFsTemplatesCommandHandler(IFsTemplateRepository repository, IFsUnitScopeProvider scopes, IUnitOfWork unitOfWork, ICurrentUser currentUser, IFsCheckRuleRepository ruleRepository)
    {
        _ruleRepository = ruleRepository;
        _repository = repository;
        _scopes = scopes;
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
    }

    public async Task<IReadOnlyList<string>> Handle(SeedDefaultFsTemplatesCommand request, CancellationToken cancellationToken)
    {
        // قالب‌های پیش‌فرض همیشه «مشترک»‌اند ⇒ فقط ستاد (۴۰۳ برای بقیه).
        (await _scopes.GetAsync(request.VahedCode, cancellationToken)).EnsureCanEdit(null);

        var now = DateTime.UtcNow;
        var userId = _currentUser.UserId;
        var created = new List<string>();

        foreach (var def in FsDefaultTemplates.All)
        {
            if (await _repository.TemplateCodeExistsAsync(null, def.Code, cancellationToken))
            {
                continue;
            }

            var template = new TB_FS_TEMPLATE
            {
                ID = Guid.NewGuid(),
                FRAMEWORK = def.Framework,
                CODE = def.Code,
                TITLE_FA = def.TitleFa,
                TITLE_EN = def.TitleEn,
                STATEMENT_TYPE = def.StatementType,
                ORDER_NO = def.OrderNo,
                CREATEDDATE = now,
                ADDUSERID = userId,
            };

            FsTemplateRules.ApplyNoteLink(template, def.NoteParentTemplateCode, def.NoteParentRowCode, def.NoteTotalRowCode);

            var version = new TB_FS_TEMPLATE_VERSION
            {
                ID = Guid.NewGuid(),
                TEMPLATE_ID = template.ID,
                VERSION_NO = 1,
                STATE = FsTemplateVersionState.Draft,
                DESCRIPTION = "قالب پیش‌فرض سیستم — پیش از فعال‌سازی، انتخاب‌گرها را با کدینگ واقعی تطبیق دهید.",
                CREATEDDATE = now,
                ADDUSERID = userId,
            };

            await _repository.AddTemplateAsync(template, cancellationToken);
            await _repository.AddVersionAsync(version, cancellationToken);
            await _repository.AddRowsAsync(FsTemplateRules.BuildRows(version.ID, def.Rows, userId, now), cancellationToken);
            created.Add(def.Code);
        }

        // بخش ۴۵-ه — قواعد کنترل پیش‌فرض (مشترک)؛ کد موجود نادیده گرفته می‌شود.
        foreach (var rule in FsDefaultTemplates.Rules)
        {
            if (await _ruleRepository.CodeExistsAsync(null, rule.Framework, rule.Code, cancellationToken))
            {
                continue;
            }

            await _ruleRepository.AddAsync(new TB_FS_CHECK_RULE
            {
                ID = Guid.NewGuid(),
                VAHEDCODE = null,
                FRAMEWORK = rule.Framework,
                CODE = rule.Code,
                TITLE_FA = rule.TitleFa,
                LEFT_EXPR = rule.LeftExpr,
                RIGHT_EXPR = rule.RightExpr,
                TOLERANCE = 0,
                SEVERITY = FsCheckSeverity.Blocking,
                IS_ACTIVE = true,
                CREATEDDATE = now,
                ADDUSERID = userId,
            }, cancellationToken);
            created.Add($"{rule.Framework}:{rule.Code}");
        }

        if (created.Count > 0)
        {
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }

        return created;
    }
}
