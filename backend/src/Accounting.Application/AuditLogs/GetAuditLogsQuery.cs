using System.Text.Json.Serialization;
using Accounting.Application.Common;
using Accounting.Application.Common.Security;
using Accounting.Domain.ValueObjects;
using FluentValidation;
using MediatR;

namespace Accounting.Application.AuditLogs;

/// <summary>
/// جست‌وجوی لاگ تغییرات — فقط نقش مدیر مالی (<see cref="AppRoles.Audit"/>، در <c>RoleAuthorizationBehavior</c>)، و
/// فقط واحد جاری یا واحد و زیرمجموعه (<see cref="ReportUnitScopeBehavior{TRequest,TResponse}"/>).
/// </summary>
/// <param name="FromDate">از این لحظه (UTC، شامل).</param>
/// <param name="ToDate">تا این لحظه (UTC، ناشامل).</param>
/// <param name="UserId">کد ملی کاربر.</param>
/// <param name="RecordId">تاریخچهٔ یک رکورد: خودش و همهٔ زیرمجموعه‌هایش (مثلاً سند و ردیف‌هایش).</param>
/// <param name="OperationId">همهٔ تغییرات یک ذخیره.</param>
/// <param name="Search">بخشی از برچسب رکورد (مثلاً شمارهٔ سند) یا نام کاربر.</param>
public sealed record GetAuditLogsQuery(
    DateTime? FromDate,
    DateTime? ToDate,
    string? UserId,
    string? TableName,
    string? ActionType,
    Guid? RecordId,
    Guid? OperationId,
    string? Search,
    int PageNumber = 1,
    int PageSize = 50) : IRequest<PagedResult<AuditLogDto>>, IMultiUnitReportQuery
{
    public ReportUnitScopeMode UnitScope { get; init; }

    public UnitCategory? UnitCategory => null;

    [JsonIgnore]
    public string VahedCode { get; set; } = string.Empty;
}

public sealed record AuditLogDto(
    Guid Id,
    DateTime? ChangeDate,
    string ActionType,
    string TableName,
    string TableTitle,
    Guid RecordId,
    string? RecordLabel,
    Guid? ParentId,
    string? ColumnName,
    string? OldValue,
    string? NewValue,
    string UserId,
    string? UserFullName,
    string? VahedCode,
    string? Year,
    string? IpAddress,
    Guid? OperationId);

public sealed record AuditLogFilter(
    IReadOnlyList<string> VahedCodes,
    DateTime? FromDate,
    DateTime? ToDate,
    string? UserId,
    string? TableName,
    string? ActionType,
    Guid? RecordId,
    Guid? OperationId,
    string? Search);

public interface IAuditLogReadRepository
{
    Task<PagedResult<AuditLogDto>> SearchAsync(AuditLogFilter filter, int pageNumber, int pageSize, CancellationToken cancellationToken = default);
}

public sealed class GetAuditLogsQueryValidator : AbstractValidator<GetAuditLogsQuery>
{
    public const int MaxPageSize = 200;

    public GetAuditLogsQueryValidator()
    {
        RuleFor(x => x.UnitScope)
            .Must(s => s is ReportUnitScopeMode.Self or ReportUnitScopeMode.WithSubUnits)
            .WithMessage("لاگ تغییرات فقط برای واحد جاری یا واحد و زیرمجموعه قابل مشاهده است.");
        RuleFor(x => x.PageNumber).GreaterThanOrEqualTo(1);
        RuleFor(x => x.PageSize).InclusiveBetween(1, MaxPageSize);
        RuleFor(x => x.ToDate).GreaterThan(x => x.FromDate)
            .When(x => x.FromDate is not null && x.ToDate is not null)
            .WithMessage("«تا تاریخ» باید بعد از «از تاریخ» باشد.");
        RuleFor(x => x.UserId).MaximumLength(10);
        RuleFor(x => x.Search).MaximumLength(100);
    }
}

public sealed class GetAuditLogsQueryHandler : IRequestHandler<GetAuditLogsQuery, PagedResult<AuditLogDto>>
{
    private readonly IAuditLogReadRepository _repository;
    private readonly IReportUnitScope _scope;

    public GetAuditLogsQueryHandler(IAuditLogReadRepository repository, IReportUnitScope scope)
    {
        _repository = repository;
        _scope = scope;
    }

    public Task<PagedResult<AuditLogDto>> Handle(GetAuditLogsQuery request, CancellationToken cancellationToken)
    {
        var filter = new AuditLogFilter(
            _scope.VahedCodes ?? [request.VahedCode],
            request.FromDate,
            request.ToDate,
            Clean(request.UserId),
            Clean(request.TableName),
            Clean(request.ActionType),
            request.RecordId,
            request.OperationId,
            Clean(request.Search));
        return _repository.SearchAsync(filter, request.PageNumber, request.PageSize, cancellationToken);
    }

    private static string? Clean(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}

/// <summary>فهرست جدول‌ها با عنوان فارسی، برای فیلتر صفحهٔ لاگ تغییرات.</summary>
public sealed record GetAuditTablesQuery : IRequest<IReadOnlyList<AuditTableDto>>;

public sealed record AuditTableDto(string Table, string Title);

public sealed class GetAuditTablesQueryHandler : IRequestHandler<GetAuditTablesQuery, IReadOnlyList<AuditTableDto>>
{
    public Task<IReadOnlyList<AuditTableDto>> Handle(GetAuditTablesQuery request, CancellationToken cancellationToken)
        => Task.FromResult<IReadOnlyList<AuditTableDto>>(
            AuditTableCatalog.All.Select(t => new AuditTableDto(t.Table, t.Title)).OrderBy(t => t.Title, StringComparer.Ordinal).ToList());
}
