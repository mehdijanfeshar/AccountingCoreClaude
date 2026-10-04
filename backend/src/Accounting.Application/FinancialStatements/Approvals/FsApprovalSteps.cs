using Accounting.Application.FinancialStatements.Access;
using System.Text.Json.Serialization;
using Accounting.Application.Common.Exceptions;
using Accounting.Application.Common.Interfaces;
using Accounting.Application.Common.Security;
using Accounting.Application.FinancialStatements.Engine;
using Accounting.Domain.Entity;
using Accounting.Domain.ValueObjects;
using FluentValidation;
using MediatR;

namespace Accounting.Application.FinancialStatements.Approvals;

// ح-۴ (docs/fs-module.md §۱۳) — مراحل گردش تأیید قابل تنظیم (سند منبع §۱۱). تصمیم صاحب پروژه: تأییدکنندهٔ هر
// مرحله = فهرست کد کاربری آن مرحله. بدون هیچ مرحله‌ای، گردش همان تأیید تک‌مرحله‌ای ۴۵-ه است.

/// <summary>یک مرحلهٔ گردش. <paramref name="CanEdit"/> = واحد هدر مالک است (مشترک = فقط ستاد).</summary>
public sealed record FsApprovalStepDto(
    Guid Id,
    string? OwnerVahedCode,
    bool CanEdit,
    FsFramework Framework,
    int StepNo,
    string TitleFa,
    IReadOnlyList<string> ApproverUserIds,
    bool IsActive);

/// <summary>پیشرفت یک مرحله در یک اجرا (برای نوار گردش).</summary>
public sealed record FsRunApprovalStepDto(
    int StepNo,
    string TitleFa,
    IReadOnlyList<string> ApproverUserIds,
    string? ApprovedBy,
    DateTime? ApprovedDate,
    bool IsCurrent,
    bool CanApprove,
    string? CannotApproveReason);

public static class FsApprovalChain
{
    /// <summary>
    /// زنجیرهٔ مراحل فعال برای اجرایی از مجموعهٔ <paramref name="framework"/> در دامنهٔ <paramref name="scope"/>:
    /// مراحل نزدیک‌ترین مالکی (خود واحد، والد، …، مشترک) که حداقل یک مرحلهٔ فعال دارد، به ترتیب شماره.
    /// </summary>
    public static IReadOnlyList<TB_FS_APPROVAL_STEP> Resolve(IEnumerable<TB_FS_APPROVAL_STEP> steps, FsUnitScope scope, FsFramework framework)
    {
        var owner = steps
            .Where(s => s.IS_ACTIVE && s.FRAMEWORK == framework)
            .Select(s => (Step: s, Priority: scope.PriorityOf(s.VAHEDCODE)))
            .Where(x => x.Priority is not null)
            .GroupBy(x => x.Priority!.Value)
            .OrderBy(g => g.Key)
            .FirstOrDefault();

        return owner is null ? [] : owner.Select(x => x.Step).OrderBy(s => s.STEP_NO).ToList();
    }

    public static IReadOnlyList<string> Approvers(string? text)
        => string.IsNullOrWhiteSpace(text)
            ? []
            : text.Split([',', '،', ' ', ';', '\n'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Select(FsText.NormalizeDigits)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

    /// <summary>
    /// چرا <paramref name="user"/> نمی‌تواند مرحلهٔ <paramref name="step"/> را تأیید کند؛ <c>null</c> = می‌تواند.
    /// <paramref name="earlierApprovers"/> = تأییدکنندگان مراحل قبلِ همین دور (یک نفر دو مرحله را تأیید نمی‌کند).
    /// </summary>
    public static string? CannotApprove(
        TB_FS_APPROVAL_STEP? step, string user, string preparer, string? submitter, IEnumerable<string> earlierApprovers)
    {
        if (user == preparer || user == submitter)
        {
            return "تهیه‌کننده یا ارسال‌کنندهٔ صورت‌ها نمی‌تواند همان اجرا را تأیید کند (تفکیک وظایف).";
        }

        if (earlierApprovers.Contains(user, StringComparer.OrdinalIgnoreCase))
        {
            return "شما مرحلهٔ قبلی همین اجرا را تأیید کرده‌اید؛ مرحلهٔ بعد را کاربر دیگری تأیید کند.";
        }

        if (step is not null)
        {
            var allowed = Approvers(step.APPROVER_USERIDS);

            if (allowed.Count > 0 && !allowed.Contains(user, StringComparer.OrdinalIgnoreCase))
            {
                return $"تأیید مرحلهٔ «{step.TITLE_FA}» فقط با {string.Join("، ", allowed)} است.";
            }
        }

        return null;
    }
}

/// <summary><c>GET api/fs/approval-steps?framework=</c> — مراحل قابل دید برای واحد هدر (مشترک + خود و بالادستی‌ها).</summary>
public sealed record GetFsApprovalStepsQuery(FsFramework? Framework) : IRequest<IReadOnlyList<FsApprovalStepDto>>, IVahedScopedQuery
{
    [JsonIgnore]
    public string VahedCode { get; set; } = string.Empty;
}

/// <summary><c>POST api/fs/approval-steps</c> — <paramref name="Shared"/> = مشترک (فقط ستاد)، وگرنه اختصاصی واحد هدر.</summary>
[FsRequires(FsOperation.EditTemplate)]
public sealed record CreateFsApprovalStepCommand(
    FsFramework Framework,
    bool Shared,
    int StepNo,
    string TitleFa,
    string? ApproverUserIds,
    bool IsActive = true) : IRequest<Guid>, IVahedScopedCommand
{
    [JsonIgnore]
    public string VahedCode { get; set; } = string.Empty;
}

/// <summary><c>POST api/fs/approval-steps/{id}/update</c></summary>
[FsRequires(FsOperation.EditTemplate)]
public sealed record UpdateFsApprovalStepCommand(Guid Id, int StepNo, string TitleFa, string? ApproverUserIds, bool IsActive)
    : IRequest, IVahedScopedCommand
{
    [JsonIgnore]
    public string VahedCode { get; set; } = string.Empty;
}

/// <summary><c>POST api/fs/approval-steps/{id}/delete</c> — حذف نرم.</summary>
[FsRequires(FsOperation.EditTemplate)]
public sealed record DeleteFsApprovalStepCommand(Guid Id) : IRequest, IVahedScopedCommand
{
    [JsonIgnore]
    public string VahedCode { get; set; } = string.Empty;
}

internal static class FsApprovalStepValidation
{
    public static void Apply<T>(
        AbstractValidator<T> v,
        System.Linq.Expressions.Expression<Func<T, int>> stepNo,
        System.Linq.Expressions.Expression<Func<T, string>> title,
        System.Linq.Expressions.Expression<Func<T, string?>> approvers)
    {
        v.RuleFor(stepNo).InclusiveBetween(1, 20).WithMessage("شمارهٔ مرحله بین ۱ و ۲۰.");
        v.RuleFor(title).NotEmpty().WithMessage("عنوان مرحله لازم است.").MaximumLength(200);
        v.RuleFor(approvers)
            .MaximumLength(500)
            .Must(a => FsApprovalChain.Approvers(a).All(u => u.Length <= 10))
            .WithMessage("هر کد کاربری حداکثر ۱۰ نویسه است.");
    }
}

public sealed class CreateFsApprovalStepCommandValidator : AbstractValidator<CreateFsApprovalStepCommand>
{
    public CreateFsApprovalStepCommandValidator()
    {
        RuleFor(x => x.Framework).IsInEnum();
        FsApprovalStepValidation.Apply(this, x => x.StepNo, x => x.TitleFa, x => x.ApproverUserIds);
    }
}

public sealed class UpdateFsApprovalStepCommandValidator : AbstractValidator<UpdateFsApprovalStepCommand>
{
    public UpdateFsApprovalStepCommandValidator()
    {
        FsApprovalStepValidation.Apply(this, x => x.StepNo, x => x.TitleFa, x => x.ApproverUserIds);
    }
}

public sealed class FsApprovalStepHandlers :
    IRequestHandler<GetFsApprovalStepsQuery, IReadOnlyList<FsApprovalStepDto>>,
    IRequestHandler<CreateFsApprovalStepCommand, Guid>,
    IRequestHandler<UpdateFsApprovalStepCommand>,
    IRequestHandler<DeleteFsApprovalStepCommand>
{
    private readonly IFsApprovalStepRepository _repository;
    private readonly IFsUnitScopeProvider _scopes;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUser _currentUser;

    public FsApprovalStepHandlers(IFsApprovalStepRepository repository, IFsUnitScopeProvider scopes, IUnitOfWork unitOfWork, ICurrentUser currentUser)
    {
        _repository = repository;
        _scopes = scopes;
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
    }

    public async Task<IReadOnlyList<FsApprovalStepDto>> Handle(GetFsApprovalStepsQuery request, CancellationToken cancellationToken)
    {
        var scope = await _scopes.GetAsync(request.VahedCode, cancellationToken);

        return (await _repository.GetAllAsync(request.Framework, cancellationToken))
            .Where(s => scope.CanSee(s.VAHEDCODE))
            .Select(s => new FsApprovalStepDto(
                s.ID, s.VAHEDCODE, scope.CanEdit(s.VAHEDCODE), s.FRAMEWORK, s.STEP_NO, s.TITLE_FA,
                FsApprovalChain.Approvers(s.APPROVER_USERIDS), s.IS_ACTIVE))
            .ToList();
    }

    public async Task<Guid> Handle(CreateFsApprovalStepCommand request, CancellationToken cancellationToken)
    {
        var owner = request.Shared ? null : request.VahedCode;
        (await _scopes.GetAsync(request.VahedCode, cancellationToken)).EnsureCanEdit(owner);

        var step = new TB_FS_APPROVAL_STEP
        {
            ID = Guid.NewGuid(),
            VAHEDCODE = owner,
            FRAMEWORK = request.Framework,
            STEP_NO = request.StepNo,
            TITLE_FA = request.TitleFa.Trim(),
            APPROVER_USERIDS = Join(request.ApproverUserIds),
            IS_ACTIVE = request.IsActive,
            CREATEDDATE = DateTime.UtcNow,
            ADDUSERID = _currentUser.UserId,
        };

        await _repository.AddAsync(step, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return step.ID;
    }

    public async Task Handle(UpdateFsApprovalStepCommand request, CancellationToken cancellationToken)
    {
        var step = await LoadEditableAsync(request.Id, request.VahedCode, cancellationToken);

        step.STEP_NO = request.StepNo;
        step.TITLE_FA = request.TitleFa.Trim();
        step.APPROVER_USERIDS = Join(request.ApproverUserIds);
        step.IS_ACTIVE = request.IsActive;
        step.CHANGEUSERID = _currentUser.UserId;
        step.UPDATEDDATE = DateTime.UtcNow;

        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task Handle(DeleteFsApprovalStepCommand request, CancellationToken cancellationToken)
    {
        var step = await LoadEditableAsync(request.Id, request.VahedCode, cancellationToken);

        step.ISDELETED = true;
        step.CHANGEUSERID = _currentUser.UserId;
        step.UPDATEDDATE = DateTime.UtcNow;

        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    private static string? Join(string? approvers)
    {
        var list = FsApprovalChain.Approvers(approvers);
        return list.Count == 0 ? null : string.Join(",", list);
    }

    private async Task<TB_FS_APPROVAL_STEP> LoadEditableAsync(Guid id, string vahedCode, CancellationToken cancellationToken)
    {
        var step = await _repository.GetForUpdateAsync(id, cancellationToken) ?? throw new NotFoundException("FsApprovalStep", id);
        var scope = await _scopes.GetAsync(vahedCode, cancellationToken);

        if (!scope.CanSee(step.VAHEDCODE))
        {
            throw new NotFoundException("FsApprovalStep", id);
        }

        scope.EnsureCanEdit(step.VAHEDCODE);
        return step;
    }
}
