using Accounting.Application.FinancialStatements.Access;
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

namespace Accounting.Application.FinancialStatements.Ratios;

// ح-۸ (docs/fs-module.md §۱۳) — تحلیل و نسبت‌ها (سند منبع §۱۲-۳): «فرمول نسبت‌ها با همان زبان فرمول قالب تعریف
// می‌شود». نسبت روی Snapshot اجرا ارزیابی می‌شود، ستون جاری و سال قبل جدا؛ روند چندساله از آخرین اجرای هر سال.

public sealed record FsRatioDto(
    Guid Id,
    string? OwnerVahedCode,
    bool CanEdit,
    FsFramework Framework,
    string Code,
    string TitleFa,
    string NumeratorExpr,
    string? DenominatorExpr,
    FsRatioFormat Format,
    int OrderNo,
    bool IsActive);

/// <summary>مقدار یک نسبت در یک اجرا. درصد به‌صورت عدد درصد (۱۲٫۵ = ۱۲٫۵٪). <paramref name="Error"/> = ارزیابی نشد.</summary>
public sealed record FsRatioValueDto(string Code, string TitleFa, FsRatioFormat Format, decimal? Current, decimal? Prior, string? Error);

/// <summary>یک سال در روند: آخرین اجرای منتشرشده (وگرنه آخرین اجرا) و مقدار نسبت‌ها.</summary>
public sealed record FsRatioTrendYearDto(string Year, Guid RunId, int RunNo, FsRunState State, IReadOnlyList<FsRatioValueDto> Values);

public static class FsRatioEngine
{
    /// <summary>نسبت‌های مؤثر: برای هر کد، نزدیک‌ترین مالک (خود واحد، والد، …، مشترک)؛ فقط فعال‌ها، به ترتیب.</summary>
    public static IReadOnlyList<TB_FS_RATIO> Effective(IEnumerable<TB_FS_RATIO> all, FsUnitScope scope, FsFramework framework)
        => all
            .Where(r => r.IS_ACTIVE && r.FRAMEWORK == framework)
            .Select(r => (Ratio: r, Priority: scope.PriorityOf(r.VAHEDCODE)))
            .Where(x => x.Priority is not null)
            .GroupBy(x => x.Ratio.CODE, StringComparer.OrdinalIgnoreCase)
            .Select(g => g.OrderBy(x => x.Priority).First().Ratio)
            .OrderBy(r => r.ORDER_NO)
            .ThenBy(r => r.CODE, StringComparer.Ordinal)
            .ToList();

    public static IReadOnlyList<FsRatioValueDto> Evaluate(IReadOnlyList<TB_FS_RATIO> ratios, FsRunDetailDto run)
    {
        var cur = new Dictionary<(string, string), decimal>();
        var prv = new Dictionary<(string, string), decimal>();

        foreach (var s in run.Statements)
        {
            foreach (var r in s.Rows)
            {
                var sign = r.NormalBalance == FsNormalBalance.Credit ? -1m : 1m;
                var key = (s.TemplateCode.ToUpperInvariant(), r.Code.ToUpperInvariant());
                cur[key] = (r.AmountCur ?? 0m) * sign;
                prv[key] = (r.AmountPrv ?? 0m) * sign;
            }
        }

        return ratios.Select(r =>
        {
            try
            {
                return new FsRatioValueDto(
                    r.CODE, r.TITLE_FA, r.FORMAT,
                    Value(r, cur),
                    run.Run.HasPrior ? Value(r, prv) : null,
                    null);
            }
            catch (Exception ex) when (ex is FsExpressionException or FsEngineException)
            {
                return new FsRatioValueDto(r.CODE, r.TITLE_FA, r.FORMAT, null, null, ex.Message);
            }
        }).ToList();
    }

    private static decimal? Value(TB_FS_RATIO r, IReadOnlyDictionary<(string, string), decimal> values)
    {
        var num = Eval(FsFormula.Parse(r.NUMERATOR_EXPR).Root, values);

        if (string.IsNullOrWhiteSpace(r.DENOMINATOR_EXPR))
        {
            return num;
        }

        var den = Eval(FsFormula.Parse(r.DENOMINATOR_EXPR).Root, values);

        if (den == 0)
        {
            return null;
        }

        var v = num / den;
        return r.FORMAT == FsRatioFormat.Percent ? v * 100m : v;
    }

    private static decimal Eval(FsExpr e, IReadOnlyDictionary<(string, string), decimal> values)
    {
        decimal Sub(FsExpr x) => Eval(x, values);

        return e switch
        {
            FsNumberExpr n => n.Value,
            FsStatementRefExpr s => values.TryGetValue((s.TemplateCode.ToUpperInvariant(), s.RowCode.ToUpperInvariant()), out var v)
                ? v
                : throw new FsEngineException($"صورت/ردیف «{s.TemplateCode} / {s.RowCode}» در این اجرا نیست."),
            FsUnaryExpr u => -Sub(u.Operand),
            FsBinaryExpr { Op: '+' } b => Sub(b.Left) + Sub(b.Right),
            FsBinaryExpr { Op: '-' } b => Sub(b.Left) - Sub(b.Right),
            FsBinaryExpr { Op: '*' } b => Sub(b.Left) * Sub(b.Right),
            FsBinaryExpr { Op: '/' } b => Sub(b.Right) is var d && d == 0 ? 0 : Sub(b.Left) / d,
            FsFunctionExpr { Name: "ABS" } f => Math.Abs(Sub(f.Args[0])),
            FsFunctionExpr { Name: "ROUND" } f => Math.Round(Sub(f.Args[0]), (int)((FsNumberExpr)f.Args[1]).Value, MidpointRounding.AwayFromZero),
            _ => throw new FsEngineException("در نسبت فقط STMT(قالب, ردیف)، عدد، عملگرها و ABS/ROUND مجازند."),
        };
    }
}

/// <summary>نسبت‌های پیش‌فرض (ح-۸) روی کدهای قالب‌های پیش‌فرض ۴۵-الف.</summary>
internal static class FsDefaultRatios
{
    private const string Na = "PENSION.NET_ASSETS";
    private const string Ch = "PENSION.CHANGES_IN_NET_ASSETS";
    private const string Fp = "COMMERCIAL.FINANCIAL_POSITION";
    private const string Pl = "COMMERCIAL.PROFIT_OR_LOSS";

    public static readonly (FsFramework Framework, string Code, string Title, string Num, string? Den, FsRatioFormat Format)[] All =
    [
        (FsFramework.Pension, "R-01", "نسبت مصارف به منابع", $"STMT({Ch}, C99)", $"STMT({Ch}, R99)", FsRatioFormat.Percent),
        (FsFramework.Pension, "R-02", "نسبت هزینهٔ درمان به درآمد حق بیمه", $"STMT({Ch}, C08) + STMT({Ch}, C09)", $"STMT({Ch}, R01)", FsRatioFormat.Percent),
        (FsFramework.Pension, "R-03", "پوشش نقدی مستمری (ماه)", $"STMT({Na}, A01)", $"(STMT({Ch}, C04) + STMT({Ch}, C05)) / 12", FsRatioFormat.Times),
        (FsFramework.Pension, "R-04", "نسبت مطالبات به درآمد حق بیمه", $"STMT({Na}, A03)", $"STMT({Ch}, R01)", FsRatioFormat.Percent),
        (FsFramework.Pension, "R-05", "نسبت هزینه‌های اداری و کارکنان به منابع", $"STMT({Ch}, C10) + STMT({Ch}, C11) + STMT({Ch}, C12)", $"STMT({Ch}, R99)", FsRatioFormat.Percent),
        (FsFramework.Commercial, "R-01", "نسبت جاری", $"STMT({Fp}, A98)", $"STMT({Fp}, L98)", FsRatioFormat.Times),
        (FsFramework.Commercial, "R-02", "نسبت آنی", $"STMT({Fp}, A98) - STMT({Fp}, A51)", $"STMT({Fp}, L98)", FsRatioFormat.Times),
        (FsFramework.Commercial, "R-03", "نسبت بدهی", $"STMT({Fp}, L99)", $"STMT({Fp}, A99)", FsRatioFormat.Percent),
        (FsFramework.Commercial, "R-04", "حاشیهٔ سود خالص", $"STMT({Pl}, P99)", $"STMT({Pl}, P01)", FsRatioFormat.Percent),
        (FsFramework.Commercial, "R-05", "بازده دارایی‌ها", $"STMT({Pl}, P99)", $"STMT({Fp}, A99)", FsRatioFormat.Percent),
    ];
}

/// <summary><c>GET api/fs/ratios?framework=</c> — تعریف‌های قابل دید برای واحد هدر.</summary>
public sealed record GetFsRatiosQuery(FsFramework? Framework) : IRequest<IReadOnlyList<FsRatioDto>>, IVahedScopedQuery
{
    [JsonIgnore]
    public string VahedCode { get; set; } = string.Empty;
}

/// <summary><c>GET api/fs/runs/{id}/ratios</c> — نسبت‌های مؤثر روی یک اجرا.</summary>
public sealed record GetFsRunRatiosQuery(Guid RunId) : IRequest<IReadOnlyList<FsRatioValueDto>>, IVahedScopedQuery
{
    [JsonIgnore]
    public string VahedCode { get; set; } = string.Empty;
}

/// <summary>
/// <c>GET api/fs/ratios/trend?framework=&amp;toYear=&amp;years=</c> — برای هر سال تا <paramref name="ToYear"/>: آخرین
/// اجرای منتشرشدهٔ واحد هدر (وگرنه آخرین اجرای جایگزین‌نشده) و نسبت‌هایش. سال بدون اجرا حذف می‌شود.
/// </summary>
public sealed record GetFsRatioTrendQuery(FsFramework Framework, string ToYear, int Years = 5) : IRequest<IReadOnlyList<FsRatioTrendYearDto>>, IVahedScopedQuery
{
    [JsonIgnore]
    public string VahedCode { get; set; } = string.Empty;
}

[FsRequires(FsOperation.EditTemplate)]
public sealed record CreateFsRatioCommand(
    FsFramework Framework,
    bool Shared,
    string Code,
    string TitleFa,
    string NumeratorExpr,
    string? DenominatorExpr,
    FsRatioFormat Format,
    int OrderNo,
    bool IsActive = true) : IRequest<Guid>, IVahedScopedCommand
{
    [JsonIgnore]
    public string VahedCode { get; set; } = string.Empty;
}

[FsRequires(FsOperation.EditTemplate)]
public sealed record UpdateFsRatioCommand(Guid Id, string TitleFa, string NumeratorExpr, string? DenominatorExpr, FsRatioFormat Format, int OrderNo, bool IsActive)
    : IRequest, IVahedScopedCommand
{
    [JsonIgnore]
    public string VahedCode { get; set; } = string.Empty;
}

[FsRequires(FsOperation.EditTemplate)]
public sealed record DeleteFsRatioCommand(Guid Id) : IRequest, IVahedScopedCommand
{
    [JsonIgnore]
    public string VahedCode { get; set; } = string.Empty;
}

/// <summary><c>POST api/fs/ratios/seed-defaults</c> — نسبت‌های پیش‌فرض مشترک (فقط ستاد، تکرارپذیر). پاسخ = کدهای ساخته‌شده.</summary>
[FsRequires(FsOperation.EditTemplate)]
public sealed record SeedDefaultFsRatiosCommand : IRequest<IReadOnlyList<string>>, IVahedScopedCommand
{
    [JsonIgnore]
    public string VahedCode { get; set; } = string.Empty;
}

internal static class FsRatioValidation
{
    public static void Apply<T>(
        AbstractValidator<T> v,
        System.Linq.Expressions.Expression<Func<T, string>> title,
        System.Linq.Expressions.Expression<Func<T, string>> numerator,
        System.Linq.Expressions.Expression<Func<T, string?>> denominator,
        System.Linq.Expressions.Expression<Func<T, FsRatioFormat>> format)
    {
        v.RuleFor(title).NotEmpty().WithMessage("عنوان نسبت لازم است.").MaximumLength(500);
        v.RuleFor(numerator)
            .NotEmpty().WithMessage("صورت نسبت لازم است.")
            .MaximumLength(1000)
            .Custom((text, ctx) =>
            {
                if (!FsRunChecks.IsValidRuleExpression(text, out var error))
                {
                    ctx.AddFailure(error ?? "عبارت نامعتبر است.");
                }
            });
        v.RuleFor(denominator)
            .MaximumLength(1000)
            .Custom((text, ctx) =>
            {
                if (!string.IsNullOrWhiteSpace(text) && !FsRunChecks.IsValidRuleExpression(text, out var error))
                {
                    ctx.AddFailure(error ?? "عبارت نامعتبر است.");
                }
            });
        v.RuleFor(format).IsInEnum();
    }
}

public sealed class CreateFsRatioCommandValidator : AbstractValidator<CreateFsRatioCommand>
{
    public CreateFsRatioCommandValidator()
    {
        RuleFor(x => x.Framework).IsInEnum();
        RuleFor(x => x.Code).NotEmpty().MaximumLength(20).Matches("^[A-Za-z0-9_.-]+$").WithMessage("کد نسبت فقط حروف لاتین، رقم و - _ .");
        FsRatioValidation.Apply(this, x => x.TitleFa, x => x.NumeratorExpr, x => x.DenominatorExpr, x => x.Format);
    }
}

public sealed class UpdateFsRatioCommandValidator : AbstractValidator<UpdateFsRatioCommand>
{
    public UpdateFsRatioCommandValidator()
    {
        FsRatioValidation.Apply(this, x => x.TitleFa, x => x.NumeratorExpr, x => x.DenominatorExpr, x => x.Format);
    }
}

public sealed class GetFsRatioTrendQueryValidator : AbstractValidator<GetFsRatioTrendQuery>
{
    public GetFsRatioTrendQueryValidator()
    {
        RuleFor(x => x.Framework).IsInEnum();
        RuleFor(x => x.ToYear).Matches("^1[34][0-9]{2}$").WithMessage("سال مالی چهاررقمی شمسی.");
        RuleFor(x => x.Years).InclusiveBetween(1, 10);
    }
}

public sealed class FsRatioHandlers :
    IRequestHandler<GetFsRatiosQuery, IReadOnlyList<FsRatioDto>>,
    IRequestHandler<GetFsRunRatiosQuery, IReadOnlyList<FsRatioValueDto>>,
    IRequestHandler<GetFsRatioTrendQuery, IReadOnlyList<FsRatioTrendYearDto>>,
    IRequestHandler<CreateFsRatioCommand, Guid>,
    IRequestHandler<UpdateFsRatioCommand>,
    IRequestHandler<DeleteFsRatioCommand>,
    IRequestHandler<SeedDefaultFsRatiosCommand, IReadOnlyList<string>>
{
    private readonly IFsRatioRepository _repository;
    private readonly IFsRunRepository _runs;
    private readonly IFsUnitScopeProvider _scopes;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUser _currentUser;

    public FsRatioHandlers(IFsRatioRepository repository, IFsRunRepository runs, IFsUnitScopeProvider scopes, IUnitOfWork unitOfWork, ICurrentUser currentUser)
    {
        _repository = repository;
        _runs = runs;
        _scopes = scopes;
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
    }

    public async Task<IReadOnlyList<FsRatioDto>> Handle(GetFsRatiosQuery request, CancellationToken cancellationToken)
    {
        var scope = await _scopes.GetAsync(request.VahedCode, cancellationToken);

        return (await _repository.GetAllAsync(request.Framework, cancellationToken))
            .Where(r => scope.CanSee(r.VAHEDCODE))
            .Select(r => new FsRatioDto(
                r.ID, r.VAHEDCODE, scope.CanEdit(r.VAHEDCODE), r.FRAMEWORK, r.CODE, r.TITLE_FA,
                r.NUMERATOR_EXPR, r.DENOMINATOR_EXPR, r.FORMAT, r.ORDER_NO, r.IS_ACTIVE))
            .ToList();
    }

    public async Task<IReadOnlyList<FsRatioValueDto>> Handle(GetFsRunRatiosQuery request, CancellationToken cancellationToken)
    {
        var detail = await _runs.GetDetailAsync(request.RunId, request.VahedCode, cancellationToken)
            ?? throw new NotFoundException("FsRun", request.RunId);
        var ratios = FsRatioEngine.Effective(
            await _repository.GetAllAsync(detail.Run.Framework, cancellationToken),
            await _scopes.GetAsync(request.VahedCode, cancellationToken),
            detail.Run.Framework);

        return FsRatioEngine.Evaluate(ratios, detail);
    }

    public async Task<IReadOnlyList<FsRatioTrendYearDto>> Handle(GetFsRatioTrendQuery request, CancellationToken cancellationToken)
    {
        var ratios = FsRatioEngine.Effective(
            await _repository.GetAllAsync(request.Framework, cancellationToken),
            await _scopes.GetAsync(request.VahedCode, cancellationToken),
            request.Framework);
        var to = int.Parse(request.ToYear, System.Globalization.CultureInfo.InvariantCulture);
        var all = await _runs.ListAsync(request.VahedCode, null, cancellationToken);
        var result = new List<FsRatioTrendYearDto>();

        for (var y = to - request.Years + 1; y <= to; y++)
        {
            var year = y.ToString(System.Globalization.CultureInfo.InvariantCulture);
            var candidates = all.Where(r => r.Year == year && r.Framework == request.Framework && r.State != FsRunState.Superseded).ToList();
            var pick = candidates.Where(r => r.State == FsRunState.Published).OrderByDescending(r => r.CreatedDate).FirstOrDefault()
                ?? candidates.OrderByDescending(r => r.CreatedDate).FirstOrDefault();

            if (pick is null)
            {
                continue;
            }

            var detail = await _runs.GetDetailAsync(pick.Id, request.VahedCode, cancellationToken);

            if (detail is not null)
            {
                result.Add(new FsRatioTrendYearDto(year, pick.Id, pick.RunNo, pick.State, FsRatioEngine.Evaluate(ratios, detail)));
            }
        }

        return result;
    }

    public async Task<Guid> Handle(CreateFsRatioCommand request, CancellationToken cancellationToken)
    {
        var owner = request.Shared ? null : request.VahedCode;
        (await _scopes.GetAsync(request.VahedCode, cancellationToken)).EnsureCanEdit(owner);

        var code = request.Code.Trim().ToUpperInvariant();
        var exists = (await _repository.GetAllAsync(request.Framework, cancellationToken))
            .Any(r => r.VAHEDCODE == owner && string.Equals(r.CODE, code, StringComparison.OrdinalIgnoreCase));

        if (exists)
        {
            throw new FsTemplateConflictException($"نسبتی با کد «{code}» برای این مجموعه و مالک وجود دارد.");
        }

        var ratio = new TB_FS_RATIO
        {
            ID = Guid.NewGuid(),
            VAHEDCODE = owner,
            FRAMEWORK = request.Framework,
            CODE = code,
            TITLE_FA = request.TitleFa.Trim(),
            NUMERATOR_EXPR = request.NumeratorExpr.Trim(),
            DENOMINATOR_EXPR = string.IsNullOrWhiteSpace(request.DenominatorExpr) ? null : request.DenominatorExpr.Trim(),
            FORMAT = request.Format,
            ORDER_NO = request.OrderNo,
            IS_ACTIVE = request.IsActive,
            CREATEDDATE = DateTime.UtcNow,
            ADDUSERID = _currentUser.UserId,
        };

        await _repository.AddAsync(ratio, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return ratio.ID;
    }

    public async Task Handle(UpdateFsRatioCommand request, CancellationToken cancellationToken)
    {
        var ratio = await LoadEditableAsync(request.Id, request.VahedCode, cancellationToken);

        ratio.TITLE_FA = request.TitleFa.Trim();
        ratio.NUMERATOR_EXPR = request.NumeratorExpr.Trim();
        ratio.DENOMINATOR_EXPR = string.IsNullOrWhiteSpace(request.DenominatorExpr) ? null : request.DenominatorExpr.Trim();
        ratio.FORMAT = request.Format;
        ratio.ORDER_NO = request.OrderNo;
        ratio.IS_ACTIVE = request.IsActive;
        ratio.CHANGEUSERID = _currentUser.UserId;
        ratio.UPDATEDDATE = DateTime.UtcNow;

        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task Handle(DeleteFsRatioCommand request, CancellationToken cancellationToken)
    {
        var ratio = await LoadEditableAsync(request.Id, request.VahedCode, cancellationToken);

        ratio.ISDELETED = true;
        ratio.CHANGEUSERID = _currentUser.UserId;
        ratio.UPDATEDDATE = DateTime.UtcNow;

        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<string>> Handle(SeedDefaultFsRatiosCommand request, CancellationToken cancellationToken)
    {
        (await _scopes.GetAsync(request.VahedCode, cancellationToken)).EnsureCanEdit(null);

        var existing = (await _repository.GetAllAsync(null, cancellationToken))
            .Where(r => r.VAHEDCODE is null)
            .Select(r => (r.FRAMEWORK, r.CODE.ToUpperInvariant()))
            .ToHashSet();
        var created = new List<string>();
        var now = DateTime.UtcNow;
        var order = 0;

        foreach (var d in FsDefaultRatios.All)
        {
            order += 10;

            if (existing.Contains((d.Framework, d.Code)))
            {
                continue;
            }

            await _repository.AddAsync(new TB_FS_RATIO
            {
                ID = Guid.NewGuid(),
                FRAMEWORK = d.Framework,
                CODE = d.Code,
                TITLE_FA = d.Title,
                NUMERATOR_EXPR = d.Num,
                DENOMINATOR_EXPR = d.Den,
                FORMAT = d.Format,
                ORDER_NO = order,
                IS_ACTIVE = true,
                CREATEDDATE = now,
                ADDUSERID = _currentUser.UserId,
            }, cancellationToken);
            created.Add($"{d.Framework}/{d.Code}");
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return created;
    }

    private async Task<TB_FS_RATIO> LoadEditableAsync(Guid id, string vahedCode, CancellationToken cancellationToken)
    {
        var ratio = await _repository.GetForUpdateAsync(id, cancellationToken) ?? throw new NotFoundException("FsRatio", id);
        var scope = await _scopes.GetAsync(vahedCode, cancellationToken);

        if (!scope.CanSee(ratio.VAHEDCODE))
        {
            throw new NotFoundException("FsRatio", id);
        }

        scope.EnsureCanEdit(ratio.VAHEDCODE);
        return ratio;
    }
}
