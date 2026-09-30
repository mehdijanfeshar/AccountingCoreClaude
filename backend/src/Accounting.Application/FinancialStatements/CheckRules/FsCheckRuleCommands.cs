using System.Text.Json.Serialization;
using Accounting.Application.Common.Exceptions;
using Accounting.Application.Common.Interfaces;
using Accounting.Application.Common.Security;
using Accounting.Application.FinancialStatements.Engine;
using Accounting.Application.FinancialStatements.Queries;
using Accounting.Domain.Entity;
using Accounting.Domain.ValueObjects;
using FluentValidation;
using MediatR;

namespace Accounting.Application.FinancialStatements.CheckRules;

/// <summary>
/// <c>GET api/fs/check-rules?framework=</c> — قواعد کنترل تساوی بین صورت‌ها (بخش ۴۵-ه) که برای واحد هدر
/// دیدنی‌اند (مشترک، اجداد، خود، زیرمجموعه)، با <c>CanEdit</c>.
/// </summary>
public sealed record GetFsCheckRulesQuery(FsFramework? Framework) : IRequest<IReadOnlyList<FsCheckRuleDto>>, IVahedScopedQuery
{
    [JsonIgnore]
    public string VahedCode { get; set; } = string.Empty;
}

/// <summary>
/// <c>POST api/fs/check-rules</c> — قاعدهٔ تازه: <c>LeftExpr = RightExpr</c> با <c>STMT(قالب, ردیف)</c> و اختلاف
/// مجاز. <paramref name="Shared"/> = مشترک (فقط ستاد، ۴۰۳)؛ وگرنه اختصاصی واحد هدر (بر مشترکِ هم‌کد مقدم).
/// </summary>
public sealed record CreateFsCheckRuleCommand(
    FsFramework Framework,
    string Code,
    string TitleFa,
    string LeftExpr,
    string RightExpr,
    decimal Tolerance,
    FsCheckSeverity Severity,
    bool IsActive,
    bool Shared) : IRequest<Guid>, IVahedScopedCommand
{
    [JsonIgnore]
    public string VahedCode { get; set; } = string.Empty;
}

/// <summary><c>POST api/fs/check-rules/{id}/update</c> — همه‌چیز جز کد، مجموعه و مالک.</summary>
public sealed record UpdateFsCheckRuleCommand(
    Guid Id,
    string TitleFa,
    string LeftExpr,
    string RightExpr,
    decimal Tolerance,
    FsCheckSeverity Severity,
    bool IsActive) : IRequest, IVahedScopedCommand
{
    [JsonIgnore]
    public string VahedCode { get; set; } = string.Empty;
}

/// <summary><c>POST api/fs/check-rules/{id}/delete</c> — حذف نرم.</summary>
public sealed record DeleteFsCheckRuleCommand(Guid Id) : IRequest, IVahedScopedCommand
{
    [JsonIgnore]
    public string VahedCode { get; set; } = string.Empty;
}

internal static class FsRuleValidation
{
    public static void AddExpressionRules<T>(AbstractValidator<T> v, System.Linq.Expressions.Expression<Func<T, string>> expr)
    {
        v.RuleFor(expr)
            .NotEmpty()
            .MaximumLength(1000)
            .Custom((text, ctx) =>
            {
                if (!string.IsNullOrWhiteSpace(text) && !FsRunChecks.IsValidRuleExpression(text, out var error))
                {
                    ctx.AddFailure(error!);
                }
            });
    }
}

public sealed class CreateFsCheckRuleCommandValidator : AbstractValidator<CreateFsCheckRuleCommand>
{
    public CreateFsCheckRuleCommandValidator()
    {
        RuleFor(x => x.Framework).IsInEnum();
        RuleFor(x => x.Code).NotEmpty().MaximumLength(20).Matches("^[A-Za-z0-9_.-]+$").WithMessage("کد کنترل لاتین، مثل V-04.");
        RuleFor(x => x.TitleFa).NotEmpty().MaximumLength(500);
        FsRuleValidation.AddExpressionRules(this, x => x.LeftExpr);
        FsRuleValidation.AddExpressionRules(this, x => x.RightExpr);
        RuleFor(x => x.Tolerance).GreaterThanOrEqualTo(0);
        RuleFor(x => x.Severity).IsInEnum();
    }
}

public sealed class UpdateFsCheckRuleCommandValidator : AbstractValidator<UpdateFsCheckRuleCommand>
{
    public UpdateFsCheckRuleCommandValidator()
    {
        RuleFor(x => x.Id).NotEqual(Guid.Empty);
        RuleFor(x => x.TitleFa).NotEmpty().MaximumLength(500);
        FsRuleValidation.AddExpressionRules(this, x => x.LeftExpr);
        FsRuleValidation.AddExpressionRules(this, x => x.RightExpr);
        RuleFor(x => x.Tolerance).GreaterThanOrEqualTo(0);
        RuleFor(x => x.Severity).IsInEnum();
    }
}

public sealed class FsCheckRuleHandlers :
    IRequestHandler<GetFsCheckRulesQuery, IReadOnlyList<FsCheckRuleDto>>,
    IRequestHandler<CreateFsCheckRuleCommand, Guid>,
    IRequestHandler<UpdateFsCheckRuleCommand>,
    IRequestHandler<DeleteFsCheckRuleCommand>
{
    private readonly IFsCheckRuleRepository _repository;
    private readonly IFsUnitScopeProvider _scopes;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUser _currentUser;

    public FsCheckRuleHandlers(IFsCheckRuleRepository repository, IFsUnitScopeProvider scopes, IUnitOfWork unitOfWork, ICurrentUser currentUser)
    {
        _repository = repository;
        _scopes = scopes;
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
    }

    public async Task<IReadOnlyList<FsCheckRuleDto>> Handle(GetFsCheckRulesQuery request, CancellationToken cancellationToken)
    {
        var scope = await _scopes.GetAsync(request.VahedCode, cancellationToken);

        return (await _repository.GetAllAsync(request.Framework, cancellationToken))
            .Where(r => scope.CanSee(r.VAHEDCODE))
            .Select(r => new FsCheckRuleDto(
                r.ID, r.VAHEDCODE, scope.CanEdit(r.VAHEDCODE), r.FRAMEWORK, r.CODE, r.TITLE_FA,
                r.LEFT_EXPR, r.RIGHT_EXPR, r.TOLERANCE, r.SEVERITY, r.IS_ACTIVE))
            .ToList();
    }

    public async Task<Guid> Handle(CreateFsCheckRuleCommand request, CancellationToken cancellationToken)
    {
        var owner = request.Shared ? null : request.VahedCode;
        (await _scopes.GetAsync(request.VahedCode, cancellationToken)).EnsureCanEdit(owner);

        var code = request.Code.Trim().ToUpperInvariant();

        if (await _repository.CodeExistsAsync(owner, request.Framework, code, cancellationToken))
        {
            throw new FsTemplateConflictException($"کنترلی با کد «{code}» برای این مجموعه و مالک قبلاً تعریف شده است.");
        }

        var rule = new TB_FS_CHECK_RULE
        {
            ID = Guid.NewGuid(),
            VAHEDCODE = owner,
            FRAMEWORK = request.Framework,
            CODE = code,
            TITLE_FA = request.TitleFa.Trim(),
            LEFT_EXPR = request.LeftExpr.Trim(),
            RIGHT_EXPR = request.RightExpr.Trim(),
            TOLERANCE = decimal.Round(request.Tolerance),
            SEVERITY = request.Severity,
            IS_ACTIVE = request.IsActive,
            CREATEDDATE = DateTime.UtcNow,
            ADDUSERID = _currentUser.UserId,
        };

        await _repository.AddAsync(rule, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return rule.ID;
    }

    public async Task Handle(UpdateFsCheckRuleCommand request, CancellationToken cancellationToken)
    {
        var rule = await LoadEditableAsync(request.Id, request.VahedCode, cancellationToken);

        rule.TITLE_FA = request.TitleFa.Trim();
        rule.LEFT_EXPR = request.LeftExpr.Trim();
        rule.RIGHT_EXPR = request.RightExpr.Trim();
        rule.TOLERANCE = decimal.Round(request.Tolerance);
        rule.SEVERITY = request.Severity;
        rule.IS_ACTIVE = request.IsActive;
        rule.CHANGEUSERID = _currentUser.UserId;
        rule.UPDATEDDATE = DateTime.UtcNow;

        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task Handle(DeleteFsCheckRuleCommand request, CancellationToken cancellationToken)
    {
        var rule = await LoadEditableAsync(request.Id, request.VahedCode, cancellationToken);

        rule.ISDELETED = true;
        rule.CHANGEUSERID = _currentUser.UserId;
        rule.UPDATEDDATE = DateTime.UtcNow;

        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    private async Task<TB_FS_CHECK_RULE> LoadEditableAsync(Guid id, string vahedCode, CancellationToken cancellationToken)
    {
        var rule = await _repository.GetForUpdateAsync(id, cancellationToken) ?? throw new NotFoundException("FsCheckRule", id);
        var scope = await _scopes.GetAsync(vahedCode, cancellationToken);

        if (!scope.CanSee(rule.VAHEDCODE))
        {
            throw new NotFoundException("FsCheckRule", id);
        }

        scope.EnsureCanEdit(rule.VAHEDCODE);
        return rule;
    }
}
