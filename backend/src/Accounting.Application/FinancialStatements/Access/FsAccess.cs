using System.Collections.Concurrent;
using System.Reflection;
using System.Text.Json.Serialization;
using Accounting.Application.Common.Exceptions;
using Accounting.Application.Common.Interfaces;
using Accounting.Application.Common.Security;
using Accounting.Application.FinancialStatements.Periods;
using Accounting.Domain.Entity;
using Accounting.Domain.ValueObjects;
using FluentValidation;
using MediatR;

namespace Accounting.Application.FinancialStatements.Access;

// ط-۲ (docs/fs-module.md §۱۴) — دسترسی سه‌بُعدی صورت‌های مالی: کاربر × دامنهٔ واحد × عملیات (سند منبع §۱۴).
// تصمیم صاحب پروژه: جدول دسترسی جدا؛ تا وقتی هیچ ردیفی تعریف نشده همه مثل قبل مجازند (فقط تفکیک واحد).

/// <summary>عملیات لازم برای یک درخواست صورت‌های مالی؛ بدون این ویژگی = «مشاهده».</summary>
[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public sealed class FsRequiresAttribute : Attribute
{
    public FsRequiresAttribute(FsOperation operation)
    {
        Operation = operation;
    }

    public FsOperation Operation { get; }
}

/// <summary>بررسی دسترسی کاربر جاری روی یک واحد (نتیجه برای طول درخواست کش می‌شود).</summary>
public sealed class FsAccessService
{
    private readonly IFsPermissionRepository _permissions;
    private readonly IUnitAccessReadRepository _unitAccess;
    private readonly ICurrentUser _currentUser;
    private IReadOnlyList<TB_FS_PERMISSION>? _all;
    private FsPeriodGuard.UnitTree? _tree;

    public FsAccessService(IFsPermissionRepository permissions, IUnitAccessReadRepository unitAccess, ICurrentUser currentUser)
    {
        _permissions = permissions;
        _unitAccess = unitAccess;
        _currentUser = currentUser;
    }

    /// <summary>عملیات مجاز کاربر جاری روی <paramref name="vahedCode"/>؛ جدول خالی = همه.</summary>
    public async Task<FsOperation> GetOperationsAsync(string vahedCode, CancellationToken cancellationToken)
    {
        _all ??= await _permissions.GetAllAsync(cancellationToken);

        if (_all.Count == 0)
        {
            return AllOperations;
        }

        var mine = _all.Where(p => string.Equals(p.USERID, _currentUser.UserId, StringComparison.OrdinalIgnoreCase)).ToList();

        if (mine.Count == 0)
        {
            return FsOperation.None;
        }

        _tree ??= new FsPeriodGuard.UnitTree(await _unitAccess.GetAllUnitsAsync(cancellationToken));
        var ops = FsOperation.None;

        foreach (var p in mine)
        {
            var covers = p.VAHEDCODE == vahedCode || (p.INCLUDE_SUB && _tree.DescendantsOrSelf(p.VAHEDCODE).Contains(vahedCode));

            if (covers)
            {
                ops |= p.OPERATIONS;
            }
        }

        return ops.HasFlag(FsOperation.Admin) ? AllOperations : ops;
    }

    public async Task EnsureAsync(FsOperation operation, string vahedCode, CancellationToken cancellationToken)
    {
        var ops = await GetOperationsAsync(vahedCode, cancellationToken);

        if ((ops & operation) != operation)
        {
            throw new FsAccessDeniedException($"دسترسی «{Label(operation)}» در صورت‌های مالی واحد {vahedCode} برای شما تعریف نشده است.");
        }
    }

    public static FsOperation AllOperations => (FsOperation)255;

    public static string Label(FsOperation op) => op switch
    {
        FsOperation.View => "مشاهده",
        FsOperation.Prepare => "تهیهٔ صورت‌ها",
        FsOperation.EditTemplate => "تغییر قالب و تنظیمات",
        FsOperation.ActivateTemplate => "فعال‌سازی قالب",
        FsOperation.Approve => "تأیید",
        FsOperation.Publish => "انتشار",
        FsOperation.ClosePeriod => "بستن دوره",
        FsOperation.Admin => "مدیریت دسترسی",
        _ => op.ToString(),
    };
}

/// <summary>
/// همهٔ درخواست‌های <c>Accounting.Application.FinancialStatements</c> پس از تعیین واحد: عملیات
/// <see cref="FsRequiresAttribute"/> (پیش‌فرض «مشاهده») روی واحد هدر. ثبت پس از VahedScopeBehavior.
/// </summary>
public sealed class FsPermissionBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private static readonly ConcurrentDictionary<Type, FsOperation?> Cache = new();
    private readonly FsAccessService _access;

    public FsPermissionBehavior(FsAccessService access)
    {
        _access = access;
    }

    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        var op = Cache.GetOrAdd(typeof(TRequest), t =>
            t.Namespace?.StartsWith("Accounting.Application.FinancialStatements", StringComparison.Ordinal) == true
                ? t.GetCustomAttribute<FsRequiresAttribute>()?.Operation ?? FsOperation.View
                : null);

        if (op is { } required && request is IVahedScoped scoped && !string.IsNullOrEmpty(scoped.VahedCode))
        {
            await _access.EnsureAsync(required, scoped.VahedCode, cancellationToken);
        }

        return await next(cancellationToken);
    }
}

public sealed record FsPermissionDto(Guid Id, string UserId, string? UserName, string VahedCode, bool IncludeSub, FsOperation Operations);

/// <summary><c>GET api/fs/permissions</c> — همهٔ ردیف‌ها در دامنهٔ واحد هدر.</summary>
[FsRequires(FsOperation.Admin)]
public sealed record GetFsPermissionsQuery : IRequest<IReadOnlyList<FsPermissionDto>>, IVahedScopedQuery
{
    [JsonIgnore]
    public string VahedCode { get; set; } = string.Empty;
}

/// <summary><c>GET api/fs/permissions/me</c> — عملیات مجاز کاربر جاری روی واحد هدر (برای فرانت).</summary>
[FsRequires(FsOperation.None)]
public sealed record GetMyFsOperationsQuery : IRequest<FsOperation>, IVahedScopedQuery
{
    [JsonIgnore]
    public string VahedCode { get; set; } = string.Empty;
}

/// <summary><c>POST api/fs/permissions</c> (افزودن) یا <c>{id}/update</c>. اولین ردیف باید «مدیریت دسترسی» داشته باشد.</summary>
[FsRequires(FsOperation.Admin)]
public sealed record SaveFsPermissionCommand(Guid? Id, string UserId, string? UserName, string UnitCode, bool IncludeSub, FsOperation Operations)
    : IRequest<Guid>, IVahedScopedCommand
{
    [JsonIgnore]
    public string VahedCode { get; set; } = string.Empty;
}

[FsRequires(FsOperation.Admin)]
public sealed record DeleteFsPermissionCommand(Guid Id) : IRequest, IVahedScopedCommand
{
    [JsonIgnore]
    public string VahedCode { get; set; } = string.Empty;
}

public sealed class SaveFsPermissionCommandValidator : AbstractValidator<SaveFsPermissionCommand>
{
    public SaveFsPermissionCommandValidator()
    {
        RuleFor(x => x.UserId).NotEmpty().MaximumLength(10);
        RuleFor(x => x.UserName).MaximumLength(200);
        RuleFor(x => x.UnitCode).NotEmpty().MaximumLength(4);
        RuleFor(x => x.Operations).Must(o => o != FsOperation.None && ((int)o & ~255) == 0).WithMessage("حداقل یک عملیات را انتخاب کنید.");
    }
}

public sealed class FsPermissionHandlers :
    IRequestHandler<GetFsPermissionsQuery, IReadOnlyList<FsPermissionDto>>,
    IRequestHandler<GetMyFsOperationsQuery, FsOperation>,
    IRequestHandler<SaveFsPermissionCommand, Guid>,
    IRequestHandler<DeleteFsPermissionCommand>
{
    private readonly IFsPermissionRepository _repository;
    private readonly FsAccessService _access;
    private readonly IFsUnitScopeProvider _scopes;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUser _currentUser;

    public FsPermissionHandlers(
        IFsPermissionRepository repository, FsAccessService access, IFsUnitScopeProvider scopes, IUnitOfWork unitOfWork, ICurrentUser currentUser)
    {
        _repository = repository;
        _access = access;
        _scopes = scopes;
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
    }

    public async Task<IReadOnlyList<FsPermissionDto>> Handle(GetFsPermissionsQuery request, CancellationToken cancellationToken)
    {
        var scope = await _scopes.GetAsync(request.VahedCode, cancellationToken);
        return (await _repository.GetAllAsync(cancellationToken))
            .Where(p => scope.Accessible.Contains(p.VAHEDCODE))
            .OrderBy(p => p.USERID)
            .Select(p => new FsPermissionDto(p.ID, p.USERID, p.USERNAME, p.VAHEDCODE, p.INCLUDE_SUB, p.OPERATIONS))
            .ToList();
    }

    public Task<FsOperation> Handle(GetMyFsOperationsQuery request, CancellationToken cancellationToken)
        => _access.GetOperationsAsync(request.VahedCode, cancellationToken);

    public async Task<Guid> Handle(SaveFsPermissionCommand request, CancellationToken cancellationToken)
    {
        var scope = await _scopes.GetAsync(request.VahedCode, cancellationToken);

        if (!scope.Accessible.Contains(request.UnitCode))
        {
            throw new FsAccessDeniedException("فقط برای واحد جاری و زیرمجموعه‌اش می‌توان دسترسی تعریف کرد.");
        }

        var all = await _repository.GetAllAsync(cancellationToken);

        // جلوگیری از قفل شدن همه: اولین ردیف باید شامل «مدیریت دسترسی» برای خود کاربر جاری باشد.
        if (all.Count == 0
            && !(request.Operations.HasFlag(FsOperation.Admin) && string.Equals(request.UserId.Trim(), _currentUser.UserId, StringComparison.OrdinalIgnoreCase)))
        {
            throw new FsTemplateConflictException("اولین دسترسی باید «مدیریت دسترسی» برای خود شما باشد؛ وگرنه کسی نمی‌تواند دسترسی‌ها را اداره کند.");
        }

        var now = DateTime.UtcNow;
        TB_FS_PERMISSION row;

        if (request.Id is { } id)
        {
            row = await _repository.GetForUpdateAsync(id, cancellationToken) ?? throw new NotFoundException("FsPermission", id);
            row.CHANGEUSERID = _currentUser.UserId;
            row.UPDATEDDATE = now;
        }
        else
        {
            row = new TB_FS_PERMISSION { ID = Guid.NewGuid(), CREATEDDATE = now, ADDUSERID = _currentUser.UserId };
            await _repository.AddAsync(row, cancellationToken);
        }

        row.USERID = request.UserId.Trim();
        row.USERNAME = string.IsNullOrWhiteSpace(request.UserName) ? null : request.UserName.Trim();
        row.VAHEDCODE = request.UnitCode;
        row.INCLUDE_SUB = request.IncludeSub;
        row.OPERATIONS = request.Operations;

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return row.ID;
    }

    public async Task Handle(DeleteFsPermissionCommand request, CancellationToken cancellationToken)
    {
        var row = await _repository.GetForUpdateAsync(request.Id, cancellationToken) ?? throw new NotFoundException("FsPermission", request.Id);
        row.ISDELETED = true;
        row.CHANGEUSERID = _currentUser.UserId;
        row.UPDATEDDATE = DateTime.UtcNow;
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
