using System.Globalization;
using System.Text;
using System.Text.Json.Serialization;
using System.Xml.Linq;
using Accounting.Application.Common.Exceptions;
using Accounting.Application.Common.Interfaces;
using Accounting.Application.Common.Security;
using Accounting.Application.FinancialStatements.Access;
using Accounting.Application.FinancialStatements.Engine;
using Accounting.Application.FinancialStatements.Queries;
using Accounting.Domain.Entity;
using Accounting.Domain.ValueObjects;
using FluentValidation;
using MediatR;

namespace Accounting.Application.FinancialStatements.Consolidation;

// ط-۳ تا ط-۸ (docs/fs-module.md §۱۴) — تنظیمات مجموعه، قواعد حذف، شرکت‌های تابعه (تراز و نرخ)، کاربرگ اجرا
// و XBRL. مالکیت تنظیمات و قواعد مثل قواعد کنترل (مشترک فقط ستاد)؛ شرکت‌های تابعه مال واحد هدرند.

public static class FsSettingKeys
{
    public const string CashSelector = "CASH_SELECTOR";
    public const string RestatementSelector = "RESTATEMENT_SELECTOR";
    public const string XbrlSchemaRef = "XBRL_SCHEMA_REF";
    public const string XbrlNamespaces = "XBRL_NAMESPACES";
    public const string XbrlEntityScheme = "XBRL_ENTITY_SCHEME";
    public const string XbrlEntityId = "XBRL_ENTITY_ID";

    public static readonly IReadOnlySet<string> All = new HashSet<string>
    {
        CashSelector, RestatementSelector, XbrlSchemaRef, XbrlNamespaces, XbrlEntityScheme, XbrlEntityId,
    };

    public static readonly IReadOnlySet<string> Selectors = new HashSet<string> { CashSelector, RestatementSelector };
}

public sealed record FsSettingDto(Guid Id, string? OwnerVahedCode, bool CanEdit, FsFramework Framework, string Key, string? Value);

public sealed record FsElimRuleDto(
    Guid Id, string? OwnerVahedCode, bool CanEdit, FsFramework Framework, string Code, string TitleFa,
    string LeftSelector, string RightSelector, decimal Tolerance, bool IsActive);

public sealed record FsEntityDto(Guid Id, string Code, string TitleFa, string Currency, decimal Ownership, bool IsActive);

public sealed record FsEntityTbRowInput(
    string AccCode, string? SourceAccCode, string? SourceAccName, int AccClass,
    decimal OpeningDebtor, decimal OpeningCreditor, decimal PeriodDebtor, decimal PeriodCreditor);

public sealed record FsEntityTbDto(IReadOnlyList<FsEntityTbRowInput> Rows, decimal? OpeningRate, decimal? ClosingRate, decimal? AverageRate);

public sealed record FsXbrlMapDto(Guid Id, string TemplateCode, string RowCode, string Element, int PeriodType);

public sealed record FsWorksheetGroupDto(string Code, string? Name, int Kind);

public sealed record FsWorksheetRowDto(
    string TemplateCode, string StatementTitle, Guid RowId, string RowCode, string? TitleFa, FsRowType RowType,
    FsNormalBalance? NormalBalance, bool Bold, IReadOnlyDictionary<string, decimal?> Amounts, decimal? Total);

public sealed record FsRunElimDto(string RuleCode, string TitleFa, decimal Left, decimal Right, decimal Difference, int Status);

/// <summary>کاربرگ ترکیب/تلفیق (سند منبع §۸ و §۱۲-۳): ستون هر گروه، «حذفیات» و جمع.</summary>
public sealed record FsWorksheetDto(IReadOnlyList<FsWorksheetGroupDto> Groups, IReadOnlyList<FsWorksheetRowDto> Rows, IReadOnlyList<FsRunElimDto> Eliminations);

// ---------------- درخواست‌ها ----------------

public sealed record GetFsSettingsQuery(FsFramework? Framework) : IRequest<IReadOnlyList<FsSettingDto>>, IVahedScopedQuery
{
    [JsonIgnore]
    public string VahedCode { get; set; } = string.Empty;
}

/// <summary>مقدار خالی = حذف تنظیم این مالک.</summary>
[FsRequires(FsOperation.EditTemplate)]
public sealed record SaveFsSettingCommand(FsFramework Framework, string Key, string? Value, bool Shared) : IRequest, IVahedScopedCommand
{
    [JsonIgnore]
    public string VahedCode { get; set; } = string.Empty;
}

public sealed record GetFsElimRulesQuery(FsFramework? Framework) : IRequest<IReadOnlyList<FsElimRuleDto>>, IVahedScopedQuery
{
    [JsonIgnore]
    public string VahedCode { get; set; } = string.Empty;
}

[FsRequires(FsOperation.EditTemplate)]
public sealed record SaveFsElimRuleCommand(
    Guid? Id, FsFramework Framework, bool Shared, string Code, string TitleFa, string LeftSelector, string RightSelector, decimal Tolerance, bool IsActive)
    : IRequest<Guid>, IVahedScopedCommand
{
    [JsonIgnore]
    public string VahedCode { get; set; } = string.Empty;
}

[FsRequires(FsOperation.EditTemplate)]
public sealed record DeleteFsElimRuleCommand(Guid Id) : IRequest, IVahedScopedCommand
{
    [JsonIgnore]
    public string VahedCode { get; set; } = string.Empty;
}

public sealed record GetFsEntitiesQuery : IRequest<IReadOnlyList<FsEntityDto>>, IVahedScopedQuery
{
    [JsonIgnore]
    public string VahedCode { get; set; } = string.Empty;
}

[FsRequires(FsOperation.EditTemplate)]
public sealed record SaveFsEntityCommand(Guid? Id, string Code, string TitleFa, string Currency, decimal Ownership, bool IsActive) : IRequest<Guid>, IVahedScopedCommand
{
    [JsonIgnore]
    public string VahedCode { get; set; } = string.Empty;
}

[FsRequires(FsOperation.EditTemplate)]
public sealed record DeleteFsEntityCommand(Guid Id) : IRequest, IVahedScopedCommand
{
    [JsonIgnore]
    public string VahedCode { get; set; } = string.Empty;
}

public sealed record GetFsEntityTbQuery(Guid EntityId, string Year, int ToMonth) : IRequest<FsEntityTbDto>, IVahedScopedQuery
{
    [JsonIgnore]
    public string VahedCode { get; set; } = string.Empty;
}

/// <summary>ورود تراز (جایگزینی کامل دوره) و نرخ‌های تسعیر. کد معین مقصد باید در کدینگ سازمان باشد.</summary>
[FsRequires(FsOperation.Prepare)]
public sealed record ImportFsEntityTbCommand(
    Guid EntityId, string Year, int ToMonth, IReadOnlyList<FsEntityTbRowInput> Rows,
    decimal? OpeningRate, decimal? ClosingRate, decimal? AverageRate) : IRequest<int>, IVahedScopedCommand
{
    [JsonIgnore]
    public string VahedCode { get; set; } = string.Empty;
}

public sealed record GetFsXbrlMapsQuery : IRequest<IReadOnlyList<FsXbrlMapDto>>, IVahedScopedQuery
{
    [JsonIgnore]
    public string VahedCode { get; set; } = string.Empty;
}

[FsRequires(FsOperation.EditTemplate)]
public sealed record SaveFsXbrlMapCommand(Guid? Id, string TemplateCode, string RowCode, string Element, int PeriodType) : IRequest<Guid>, IVahedScopedCommand
{
    [JsonIgnore]
    public string VahedCode { get; set; } = string.Empty;
}

[FsRequires(FsOperation.EditTemplate)]
public sealed record DeleteFsXbrlMapCommand(Guid Id) : IRequest, IVahedScopedCommand
{
    [JsonIgnore]
    public string VahedCode { get; set; } = string.Empty;
}

public sealed record GetFsRunWorksheetQuery(Guid RunId) : IRequest<FsWorksheetDto>, IVahedScopedQuery
{
    [JsonIgnore]
    public string VahedCode { get; set; } = string.Empty;
}

public sealed record GetFsRunXbrlQuery(Guid RunId) : IRequest<FsFileDto?>, IVahedScopedQuery
{
    [JsonIgnore]
    public string VahedCode { get; set; } = string.Empty;
}

// ---------------- اعتبارسنجی ----------------

internal static class FsSelectorRule
{
    public static IRuleBuilderOptionsConditions<T, string?> ValidSelector<T>(this IRuleBuilder<T, string?> rule)
        => rule.Custom((text, ctx) =>
        {
            if (!string.IsNullOrWhiteSpace(text) && !AccountSelector.TryParse(text, out _, out var error))
            {
                ctx.AddFailure(error ?? "انتخاب‌گر نامعتبر است.");
            }
        });
}

public sealed class SaveFsSettingCommandValidator : AbstractValidator<SaveFsSettingCommand>
{
    public SaveFsSettingCommandValidator()
    {
        RuleFor(x => x.Framework).IsInEnum();
        RuleFor(x => x.Key).Must(k => FsSettingKeys.All.Contains(k)).WithMessage("کلید تنظیم ناشناخته است.");
        RuleFor(x => x.Value).MaximumLength(2000);
        RuleFor(x => x.Value).ValidSelector().When(x => FsSettingKeys.Selectors.Contains(x.Key));
    }
}

public sealed class SaveFsElimRuleCommandValidator : AbstractValidator<SaveFsElimRuleCommand>
{
    public SaveFsElimRuleCommandValidator()
    {
        RuleFor(x => x.Framework).IsInEnum();
        RuleFor(x => x.Code).NotEmpty().MaximumLength(20);
        RuleFor(x => x.TitleFa).NotEmpty().MaximumLength(500);
        RuleFor(x => x.LeftSelector).NotEmpty().MaximumLength(1000);
        RuleFor(x => x.RightSelector).NotEmpty().MaximumLength(1000);
        RuleFor(x => (string?)x.LeftSelector).ValidSelector();
        RuleFor(x => (string?)x.RightSelector).ValidSelector();
        RuleFor(x => x.Tolerance).GreaterThanOrEqualTo(0);
    }
}

public sealed class SaveFsEntityCommandValidator : AbstractValidator<SaveFsEntityCommand>
{
    public SaveFsEntityCommandValidator()
    {
        RuleFor(x => x.Code).NotEmpty().MaximumLength(4).Matches("^[A-Za-z0-9]+$").WithMessage("کد شرکت ۱ تا ۴ نویسهٔ لاتین/رقم.");
        RuleFor(x => x.TitleFa).NotEmpty().MaximumLength(500);
        RuleFor(x => x.Currency).NotEmpty().Length(3).Matches("^[A-Za-z]{3}$").WithMessage("کد ارز سه حرفی (مثل IRR، USD).");
        RuleFor(x => x.Ownership).InclusiveBetween(0, 100);
    }
}

public sealed class ImportFsEntityTbCommandValidator : AbstractValidator<ImportFsEntityTbCommand>
{
    public ImportFsEntityTbCommandValidator()
    {
        RuleFor(x => x.Year).Matches("^1[34][0-9]{2}$");
        RuleFor(x => x.ToMonth).InclusiveBetween(1, 12);
        RuleFor(x => x.Rows).NotEmpty().Must(r => r.Count <= 20000);
        RuleForEach(x => x.Rows).ChildRules(r =>
        {
            r.RuleFor(a => a.AccCode).NotEmpty().MaximumLength(20);
            r.RuleFor(a => a.AccClass).InclusiveBetween(1, 3);
        });
        RuleFor(x => x.OpeningRate).GreaterThan(0).When(x => x.OpeningRate is not null);
        RuleFor(x => x.ClosingRate).GreaterThan(0).When(x => x.ClosingRate is not null);
        RuleFor(x => x.AverageRate).GreaterThan(0).When(x => x.AverageRate is not null);
    }
}

public sealed class SaveFsXbrlMapCommandValidator : AbstractValidator<SaveFsXbrlMapCommand>
{
    public SaveFsXbrlMapCommandValidator()
    {
        RuleFor(x => x.TemplateCode).NotEmpty().MaximumLength(50);
        RuleFor(x => x.RowCode).NotEmpty().MaximumLength(20);
        RuleFor(x => x.Element).NotEmpty().MaximumLength(200).Matches("^[A-Za-z_][\\w.-]*:[A-Za-z_][\\w.-]*$").WithMessage("عنصر به شکل prefix:Name.");
        RuleFor(x => x.PeriodType).InclusiveBetween(1, 2);
    }
}

// ---------------- اجرا ----------------

public sealed class FsConsolidationHandlers :
    IRequestHandler<GetFsSettingsQuery, IReadOnlyList<FsSettingDto>>,
    IRequestHandler<SaveFsSettingCommand>,
    IRequestHandler<GetFsElimRulesQuery, IReadOnlyList<FsElimRuleDto>>,
    IRequestHandler<SaveFsElimRuleCommand, Guid>,
    IRequestHandler<DeleteFsElimRuleCommand>,
    IRequestHandler<GetFsEntitiesQuery, IReadOnlyList<FsEntityDto>>,
    IRequestHandler<SaveFsEntityCommand, Guid>,
    IRequestHandler<DeleteFsEntityCommand>,
    IRequestHandler<GetFsEntityTbQuery, FsEntityTbDto>,
    IRequestHandler<ImportFsEntityTbCommand, int>,
    IRequestHandler<GetFsXbrlMapsQuery, IReadOnlyList<FsXbrlMapDto>>,
    IRequestHandler<SaveFsXbrlMapCommand, Guid>,
    IRequestHandler<DeleteFsXbrlMapCommand>,
    IRequestHandler<GetFsRunWorksheetQuery, FsWorksheetDto>,
    IRequestHandler<GetFsRunXbrlQuery, FsFileDto?>
{
    private readonly IFsConsolidationRepository _repo;
    private readonly IFsRunRepository _runs;
    private readonly IFsBalanceReadRepository _balances;
    private readonly IFsUnitScopeProvider _scopes;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUser _currentUser;

    public FsConsolidationHandlers(
        IFsConsolidationRepository repo,
        IFsRunRepository runs,
        IFsBalanceReadRepository balances,
        IFsUnitScopeProvider scopes,
        IUnitOfWork unitOfWork,
        ICurrentUser currentUser)
    {
        _repo = repo;
        _runs = runs;
        _balances = balances;
        _scopes = scopes;
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
    }

    // --- تنظیمات ---

    public async Task<IReadOnlyList<FsSettingDto>> Handle(GetFsSettingsQuery request, CancellationToken cancellationToken)
    {
        var scope = await _scopes.GetAsync(request.VahedCode, cancellationToken);
        return (await _repo.GetSettingsAsync(request.Framework, cancellationToken))
            .Where(s => scope.CanSee(s.VAHEDCODE))
            .Select(s => new FsSettingDto(s.ID, s.VAHEDCODE, scope.CanEdit(s.VAHEDCODE), s.FRAMEWORK, s.SETTING_KEY, s.SETTING_VALUE))
            .ToList();
    }

    public async Task Handle(SaveFsSettingCommand request, CancellationToken cancellationToken)
    {
        var owner = request.Shared ? null : request.VahedCode;
        (await _scopes.GetAsync(request.VahedCode, cancellationToken)).EnsureCanEdit(owner);

        var now = DateTime.UtcNow;
        var row = await _repo.GetSettingForUpdateAsync(owner, request.Framework, request.Key, cancellationToken);

        if (row is null)
        {
            row = new TB_FS_SETTING
            {
                ID = Guid.NewGuid(),
                VAHEDCODE = owner,
                FRAMEWORK = request.Framework,
                SETTING_KEY = request.Key,
                CREATEDDATE = now,
                ADDUSERID = _currentUser.UserId,
            };
            await _repo.AddSettingAsync(row, cancellationToken);
        }
        else
        {
            row.CHANGEUSERID = _currentUser.UserId;
            row.UPDATEDDATE = now;
        }

        row.SETTING_VALUE = string.IsNullOrWhiteSpace(request.Value) ? null : request.Value.Trim();
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    // --- قواعد حذف ---

    public async Task<IReadOnlyList<FsElimRuleDto>> Handle(GetFsElimRulesQuery request, CancellationToken cancellationToken)
    {
        var scope = await _scopes.GetAsync(request.VahedCode, cancellationToken);
        return (await _repo.GetElimRulesAsync(request.Framework, cancellationToken))
            .Where(r => scope.CanSee(r.VAHEDCODE))
            .Select(r => new FsElimRuleDto(r.ID, r.VAHEDCODE, scope.CanEdit(r.VAHEDCODE), r.FRAMEWORK, r.CODE, r.TITLE_FA,
                r.LEFT_SELECTOR, r.RIGHT_SELECTOR, r.TOLERANCE, r.IS_ACTIVE))
            .ToList();
    }

    public async Task<Guid> Handle(SaveFsElimRuleCommand request, CancellationToken cancellationToken)
    {
        var scope = await _scopes.GetAsync(request.VahedCode, cancellationToken);
        var now = DateTime.UtcNow;
        TB_FS_ELIM_RULE rule;

        if (request.Id is { } id)
        {
            rule = await _repo.GetElimRuleForUpdateAsync(id, cancellationToken) ?? throw new NotFoundException("FsElimRule", id);

            if (!scope.CanSee(rule.VAHEDCODE))
            {
                throw new NotFoundException("FsElimRule", id);
            }

            scope.EnsureCanEdit(rule.VAHEDCODE);
            rule.CHANGEUSERID = _currentUser.UserId;
            rule.UPDATEDDATE = now;
        }
        else
        {
            var owner = request.Shared ? null : request.VahedCode;
            scope.EnsureCanEdit(owner);
            var code = request.Code.Trim().ToUpperInvariant();

            if ((await _repo.GetElimRulesAsync(request.Framework, cancellationToken)).Any(r => r.VAHEDCODE == owner && r.CODE == code))
            {
                throw new FsTemplateConflictException($"قاعدهٔ حذفی با کد «{code}» برای این مالک و مجموعه وجود دارد.");
            }

            rule = new TB_FS_ELIM_RULE
            {
                ID = Guid.NewGuid(),
                VAHEDCODE = owner,
                FRAMEWORK = request.Framework,
                CODE = code,
                CREATEDDATE = now,
                ADDUSERID = _currentUser.UserId,
            };
            await _repo.AddElimRuleAsync(rule, cancellationToken);
        }

        rule.TITLE_FA = request.TitleFa.Trim();
        rule.LEFT_SELECTOR = AccountSelector.Parse(request.LeftSelector).ToString();
        rule.RIGHT_SELECTOR = AccountSelector.Parse(request.RightSelector).ToString();
        rule.TOLERANCE = decimal.Round(request.Tolerance);
        rule.IS_ACTIVE = request.IsActive;

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return rule.ID;
    }

    public async Task Handle(DeleteFsElimRuleCommand request, CancellationToken cancellationToken)
    {
        var rule = await _repo.GetElimRuleForUpdateAsync(request.Id, cancellationToken) ?? throw new NotFoundException("FsElimRule", request.Id);
        var scope = await _scopes.GetAsync(request.VahedCode, cancellationToken);

        if (!scope.CanSee(rule.VAHEDCODE))
        {
            throw new NotFoundException("FsElimRule", request.Id);
        }

        scope.EnsureCanEdit(rule.VAHEDCODE);
        rule.ISDELETED = true;
        rule.CHANGEUSERID = _currentUser.UserId;
        rule.UPDATEDDATE = DateTime.UtcNow;
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    // --- شرکت‌های تابعه ---

    public async Task<IReadOnlyList<FsEntityDto>> Handle(GetFsEntitiesQuery request, CancellationToken cancellationToken)
        => (await _repo.GetEntitiesAsync(cancellationToken))
            .Where(e => e.VAHEDCODE == request.VahedCode)
            .Select(e => new FsEntityDto(e.ID, e.CODE, e.TITLE_FA, e.CURRENCY, e.OWNERSHIP, e.IS_ACTIVE))
            .ToList();

    public async Task<Guid> Handle(SaveFsEntityCommand request, CancellationToken cancellationToken)
    {
        var scope = await _scopes.GetAsync(request.VahedCode, cancellationToken);
        var code = request.Code.Trim().ToUpperInvariant();

        if (scope.Names.ContainsKey(code) || code == FsSyntheticAccounts.EliminationGroup)
        {
            throw new FsTemplateConflictException($"کد «{code}» کد یک واحد سازمان (یا رزرو) است؛ کد دیگری انتخاب کنید.");
        }

        var all = await _repo.GetEntitiesAsync(cancellationToken);

        if (all.Any(e => e.CODE == code && e.ID != request.Id))
        {
            throw new FsTemplateConflictException($"شرکت تابعه‌ای با کد «{code}» وجود دارد.");
        }

        var now = DateTime.UtcNow;
        TB_FS_ENTITY entity;

        if (request.Id is { } id)
        {
            entity = await LoadEntityAsync(id, request.VahedCode, cancellationToken);
            entity.CHANGEUSERID = _currentUser.UserId;
            entity.UPDATEDDATE = now;
        }
        else
        {
            entity = new TB_FS_ENTITY { ID = Guid.NewGuid(), VAHEDCODE = request.VahedCode, CREATEDDATE = now, ADDUSERID = _currentUser.UserId };
            await _repo.AddEntityAsync(entity, cancellationToken);
        }

        entity.CODE = code;
        entity.TITLE_FA = request.TitleFa.Trim();
        entity.CURRENCY = request.Currency.Trim().ToUpperInvariant();
        entity.OWNERSHIP = request.Ownership;
        entity.IS_ACTIVE = request.IsActive;

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return entity.ID;
    }

    public async Task Handle(DeleteFsEntityCommand request, CancellationToken cancellationToken)
    {
        var entity = await LoadEntityAsync(request.Id, request.VahedCode, cancellationToken);
        entity.ISDELETED = true;
        entity.CHANGEUSERID = _currentUser.UserId;
        entity.UPDATEDDATE = DateTime.UtcNow;
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task<FsEntityTbDto> Handle(GetFsEntityTbQuery request, CancellationToken cancellationToken)
    {
        await LoadEntityAsync(request.EntityId, request.VahedCode, cancellationToken);
        var ids = new[] { request.EntityId };
        var rows = await _repo.GetEntityTbAsync(ids, request.Year, request.ToMonth, cancellationToken);
        var rate = (await _repo.GetRatesAsync(ids, request.Year, request.ToMonth, cancellationToken)).FirstOrDefault();

        return new FsEntityTbDto(
            rows.OrderBy(r => r.ACCCODE)
                .Select(r => new FsEntityTbRowInput(r.ACCCODE, r.SOURCE_ACCCODE, r.SOURCE_ACCNAME, r.ACC_CLASS,
                    r.OPENING_DEBTOR, r.OPENING_CREDITOR, r.PERIOD_DEBTOR, r.PERIOD_CREDITOR))
                .ToList(),
            rate?.OPENING_RATE, rate?.CLOSING_RATE, rate?.AVERAGE_RATE);
    }

    public async Task<int> Handle(ImportFsEntityTbCommand request, CancellationToken cancellationToken)
    {
        var entity = await LoadEntityAsync(request.EntityId, request.VahedCode, cancellationToken);
        var chart = (await _balances.GetChartMoeinsAsync(cancellationToken)).Select(m => m.AccCode).ToHashSet(StringComparer.Ordinal);
        var unknown = request.Rows.Select(r => FsText.NormalizeDigits(r.AccCode).Trim()).Where(c => !chart.Contains(c)).Distinct().Take(10).ToList();

        if (unknown.Count > 0)
        {
            throw new FsTemplateConflictException($"این کدهای معین در کدینگ سازمان نیستند: {string.Join("، ", unknown)} — ستون «کد معین سازمان» را نگاشت کنید.");
        }

        var now = DateTime.UtcNow;
        var rows = request.Rows.Select(r => new TB_FS_ENTITY_TB
        {
            ID = Guid.NewGuid(),
            ENTITY_ID = entity.ID,
            YEAR = request.Year,
            TO_MONTH = request.ToMonth,
            ACCCODE = FsText.NormalizeDigits(r.AccCode).Trim(),
            SOURCE_ACCCODE = string.IsNullOrWhiteSpace(r.SourceAccCode) ? null : r.SourceAccCode.Trim(),
            SOURCE_ACCNAME = string.IsNullOrWhiteSpace(r.SourceAccName) ? null : (r.SourceAccName.Trim().Length > 500 ? r.SourceAccName.Trim()[..500] : r.SourceAccName.Trim()),
            ACC_CLASS = r.AccClass,
            OPENING_DEBTOR = r.OpeningDebtor,
            OPENING_CREDITOR = r.OpeningCreditor,
            PERIOD_DEBTOR = r.PeriodDebtor,
            PERIOD_CREDITOR = r.PeriodCreditor,
            CREATEDDATE = now,
            ADDUSERID = _currentUser.UserId,
        }).ToList();

        await _repo.ReplaceEntityTbAsync(entity.ID, request.Year, request.ToMonth, rows, cancellationToken);

        if (request.OpeningRate is not null || request.ClosingRate is not null || request.AverageRate is not null)
        {
            var rate = await _repo.GetRateForUpdateAsync(entity.ID, request.Year, request.ToMonth, cancellationToken);

            if (rate is null)
            {
                rate = new TB_FS_ENTITY_RATE { ID = Guid.NewGuid(), ENTITY_ID = entity.ID, YEAR = request.Year, TO_MONTH = request.ToMonth, CREATEDDATE = now, ADDUSERID = _currentUser.UserId };
                await _repo.AddRateAsync(rate, cancellationToken);
            }

            rate.OPENING_RATE = request.OpeningRate ?? rate.OPENING_RATE;
            rate.CLOSING_RATE = request.ClosingRate ?? rate.CLOSING_RATE;
            rate.AVERAGE_RATE = request.AverageRate ?? rate.AVERAGE_RATE;
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return rows.Count;
    }

    private async Task<TB_FS_ENTITY> LoadEntityAsync(Guid id, string vahedCode, CancellationToken cancellationToken)
    {
        var entity = await _repo.GetEntityForUpdateAsync(id, cancellationToken);
        return entity is not null && entity.VAHEDCODE == vahedCode ? entity : throw new NotFoundException("FsEntity", id);
    }

    // --- XBRL ---

    public async Task<IReadOnlyList<FsXbrlMapDto>> Handle(GetFsXbrlMapsQuery request, CancellationToken cancellationToken)
        => (await _repo.GetXbrlMapsAsync(cancellationToken))
            .Select(m => new FsXbrlMapDto(m.ID, m.TEMPLATE_CODE, m.ROW_CODE, m.ELEMENT, m.PERIOD_TYPE))
            .ToList();

    public async Task<Guid> Handle(SaveFsXbrlMapCommand request, CancellationToken cancellationToken)
    {
        TB_FS_XBRL_MAP map;

        if (request.Id is { } id)
        {
            map = await _repo.GetXbrlMapForUpdateAsync(id, cancellationToken) ?? throw new NotFoundException("FsXbrlMap", id);
        }
        else
        {
            map = new TB_FS_XBRL_MAP { ID = Guid.NewGuid(), CREATEDDATE = DateTime.UtcNow, ADDUSERID = _currentUser.UserId };
            await _repo.AddXbrlMapAsync(map, cancellationToken);
        }

        map.TEMPLATE_CODE = request.TemplateCode.Trim();
        map.ROW_CODE = request.RowCode.Trim();
        map.ELEMENT = request.Element.Trim();
        map.PERIOD_TYPE = request.PeriodType;

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return map.ID;
    }

    public async Task Handle(DeleteFsXbrlMapCommand request, CancellationToken cancellationToken)
    {
        var map = await _repo.GetXbrlMapForUpdateAsync(request.Id, cancellationToken) ?? throw new NotFoundException("FsXbrlMap", request.Id);
        _repo.RemoveXbrlMap(map);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    // --- کاربرگ و خروجی XBRL اجرا ---

    public async Task<FsWorksheetDto> Handle(GetFsRunWorksheetQuery request, CancellationToken cancellationToken)
    {
        var detail = await _runs.GetDetailAsync(request.RunId, request.VahedCode, cancellationToken) ?? throw new NotFoundException("FsRun", request.RunId);
        var units = await _repo.GetRunUnitsAsync(request.RunId, request.VahedCode, cancellationToken);
        var amounts = await _repo.GetRunRowGroupsAsync(request.RunId, request.VahedCode, cancellationToken);
        var elims = await _repo.GetRunElimsAsync(request.RunId, request.VahedCode, cancellationToken);

        var names = units.GroupBy(u => u.GROUP_CODE).ToDictionary(g => g.Key, g => g.FirstOrDefault(u => u.UNIT_CODE == g.Key)?.UNIT_NAME ?? g.First().UNIT_NAME);
        var kinds = units.GroupBy(u => u.GROUP_CODE).ToDictionary(g => g.Key, g => g.Max(u => u.KIND));

        var groups = amounts.Select(a => a.GROUP_CODE).Distinct()
            .OrderBy(g => g == FsSyntheticAccounts.EliminationGroup ? 2 : kinds.GetValueOrDefault(g) == 2 ? 1 : 0)
            .ThenBy(g => g, StringComparer.Ordinal)
            .Select(g => new FsWorksheetGroupDto(
                g,
                g == FsSyntheticAccounts.EliminationGroup ? "حذفیات" : names.GetValueOrDefault(g),
                g == FsSyntheticAccounts.EliminationGroup ? 3 : kinds.GetValueOrDefault(g, 1)))
            .ToList();

        var byRow = amounts.GroupBy(a => a.RUN_ROW_ID).ToDictionary(g => g.Key, g => g.ToDictionary(a => a.GROUP_CODE, a => a.AMOUNT_CUR));
        var rows = detail.Statements
            .Where(s => !s.IsNote)
            .SelectMany(s => s.Rows.Select(r => new FsWorksheetRowDto(
                s.TemplateCode, s.TitleFa, r.Id, r.Code, r.TitleFa, r.RowType, r.NormalBalance, r.Format.Bold || r.RowType == FsRowType.Header,
                byRow.GetValueOrDefault(r.Id) ?? new Dictionary<string, decimal?>(),
                r.AmountCur)))
            .ToList();

        return new FsWorksheetDto(groups, rows, elims.Select(e => new FsRunElimDto(e.RULE_CODE, e.TITLE_FA, e.LEFT_AMOUNT, e.RIGHT_AMOUNT, e.DIFFERENCE, e.STATUS)).ToList());
    }

    /// <summary>
    /// ط-۸ — سند XBRL Instance (2.1): schemaRef از تنظیمات، دو context هویت واحد برای دورهٔ جاری و قبل (instant پایان
    /// دوره و duration ابتدا تا پایان)، واحد ریال، و برای هر ردیف نگاشت‌شده یک fact به مبلغ نمایشی.
    /// </summary>
    public async Task<FsFileDto?> Handle(GetFsRunXbrlQuery request, CancellationToken cancellationToken)
    {
        var detail = await _runs.GetDetailAsync(request.RunId, request.VahedCode, cancellationToken);

        if (detail is null)
        {
            return null;
        }

        var scope = await _scopes.GetAsync(request.VahedCode, cancellationToken);
        var settings = await _repo.GetSettingsAsync(detail.Run.Framework, cancellationToken);
        string? Setting(string key) => FsConsolidationEngine.Setting(settings, scope, detail.Run.Framework, key);
        var maps = await _repo.GetXbrlMapsAsync(cancellationToken);

        if (maps.Count == 0)
        {
            throw new FsTemplateConflictException("هیچ ردیفی به عنصر XBRL نگاشت نشده است؛ از صفحهٔ «نگاشت XBRL» نگاشت کنید.");
        }

        XNamespace xbrli = "http://www.xbrl.org/2003/instance";
        XNamespace link = "http://www.xbrl.org/2003/linkbase";
        XNamespace xlink = "http://www.w3.org/1999/xlink";
        XNamespace iso4217 = "http://www.xbrl.org/2003/iso4217";

        var prefixes = (Setting(FsSettingKeys.XbrlNamespaces) ?? string.Empty)
            .Split([';', '\n'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(p => p.Split('=', 2))
            .Where(p => p.Length == 2)
            .ToDictionary(p => p[0].Trim(), p => (XNamespace)p[1].Trim());

        var run = detail.Run;
        var year = int.Parse(run.Year, CultureInfo.InvariantCulture);

        // XBRL تاریخ میلادی (xs:date) می‌خواهد؛ روز «۳۱» پایان هر ماه به طول واقعی همان ماه شمسی بریده می‌شود.
        var persian = new PersianCalendar();
        string Iso(int y, int m, int d) =>
            persian.ToDateTime(y, m, Math.Min(d, persian.GetDaysInMonth(y, m)), 0, 0, 0, 0).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        var curStart = Iso(year, 1, 1);
        var curEnd = Iso(year, run.ToMonth, 31);
        var prvStart = Iso(year - 1, 1, 1);
        var prvEnd = Iso(year - 1, run.ToMonth, 31);

        var entityScheme = Setting(FsSettingKeys.XbrlEntityScheme) ?? "http://www.codal.ir";
        var entityId = Setting(FsSettingKeys.XbrlEntityId) ?? run.VahedCode;

        XElement Context(string id, XElement period) => new(xbrli + "context",
            new XAttribute("id", id),
            new XElement(xbrli + "entity", new XElement(xbrli + "identifier", new XAttribute("scheme", entityScheme), entityId)),
            period);

        var root = new XElement(xbrli + "xbrl",
            new XAttribute(XNamespace.Xmlns + "xbrli", xbrli),
            new XAttribute(XNamespace.Xmlns + "link", link),
            new XAttribute(XNamespace.Xmlns + "xlink", xlink),
            new XAttribute(XNamespace.Xmlns + "iso4217", iso4217),
            prefixes.Select(p => new XAttribute(XNamespace.Xmlns + p.Key, p.Value)),
            new XElement(link + "schemaRef",
                new XAttribute(xlink + "type", "simple"),
                new XAttribute(xlink + "href", Setting(FsSettingKeys.XbrlSchemaRef) ?? "taxonomy.xsd")),
            Context("CUR_I", new XElement(xbrli + "period", new XElement(xbrli + "instant", curEnd))),
            Context("CUR_D", new XElement(xbrli + "period", new XElement(xbrli + "startDate", curStart), new XElement(xbrli + "endDate", curEnd))),
            new XElement(xbrli + "unit", new XAttribute("id", "IRR"), new XElement(xbrli + "measure", "iso4217:IRR")));

        if (run.HasPrior)
        {
            root.Add(
                Context("PRV_I", new XElement(xbrli + "period", new XElement(xbrli + "instant", prvEnd))),
                Context("PRV_D", new XElement(xbrli + "period", new XElement(xbrli + "startDate", prvStart), new XElement(xbrli + "endDate", prvEnd))));
        }

        var rowsByKey = detail.Statements
            .SelectMany(s => s.Rows.Select(r => (s.TemplateCode, Row: r)))
            .ToDictionary(x => (x.TemplateCode.ToUpperInvariant(), x.Row.Code.ToUpperInvariant()), x => x.Row);
        var missingPrefix = new HashSet<string>();
        var facts = 0;

        foreach (var m in maps)
        {
            if (!rowsByKey.TryGetValue((m.TEMPLATE_CODE.ToUpperInvariant(), m.ROW_CODE.ToUpperInvariant()), out var row))
            {
                continue;
            }

            var parts = m.ELEMENT.Split(':', 2);

            if (!prefixes.TryGetValue(parts[0], out var ns))
            {
                missingPrefix.Add(parts[0]);
                continue;
            }

            var sign = row.NormalBalance == FsNormalBalance.Credit ? -1m : 1m;
            var suffix = m.PERIOD_TYPE == 1 ? "_I" : "_D";

            void Fact(string ctx, decimal? amount)
            {
                if (amount is { } a)
                {
                    root.Add(new XElement(ns + parts[1],
                        new XAttribute("contextRef", ctx + suffix),
                        new XAttribute("unitRef", "IRR"),
                        new XAttribute("decimals", "0"),
                        decimal.Round(a * sign).ToString(CultureInfo.InvariantCulture)));
                    facts++;
                }
            }

            Fact("CUR", row.AmountCur);

            if (run.HasPrior)
            {
                Fact("PRV", row.AmountPrv);
            }
        }

        if (missingPrefix.Count > 0)
        {
            throw new FsTemplateConflictException(
                $"فضای نام پیشوندهای {string.Join("، ", missingPrefix)} در تنظیم «XBRL_NAMESPACES» نیست (شکل: prefix=uri;prefix2=uri2).");
        }

        if (facts == 0)
        {
            throw new FsTemplateConflictException("هیچ ردیف نگاشت‌شده‌ای در این اجرا مقدار ندارد.");
        }

        var xml = new XDocument(new XDeclaration("1.0", "utf-8", null), root).ToString();
        return new FsFileDto($"FS-{run.RunNo}.xbrl", "application/xml", Encoding.UTF8.GetBytes("<?xml version=\"1.0\" encoding=\"utf-8\"?>\n" + xml));
    }
}
