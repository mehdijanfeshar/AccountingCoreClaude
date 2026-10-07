using Accounting.Domain.ValueObjects;
using Accounting.Application.Common.Exceptions;
using Accounting.Application.Common.Interfaces;
using MediatR;

namespace Accounting.Application.Common.Security;

/// <summary>
/// نگاشت نوع واحد (<c>TB_VAHED_TYPE.TYPECODE</c>) به گروه — تأیید صاحب پروژه ۲۰۲۶-۱۰-۰۵. بیمه‌ای: اداره‌کل (1)،
/// شعبه (9). درمانی: 2..8، مدیریت درمان (15)، طب کار (16). ستادی: 10..14 و ستاد مرکزی (17).
/// </summary>
public static class UnitCategories
{
    private static readonly Dictionary<string, UnitCategory> ByTypeCode = new(StringComparer.Ordinal)
    {
        ["1"] = UnitCategory.Insurance,
        ["9"] = UnitCategory.Insurance,
        ["2"] = UnitCategory.Medical,
        ["3"] = UnitCategory.Medical,
        ["4"] = UnitCategory.Medical,
        ["5"] = UnitCategory.Medical,
        ["6"] = UnitCategory.Medical,
        ["7"] = UnitCategory.Medical,
        ["8"] = UnitCategory.Medical,
        ["15"] = UnitCategory.Medical,
        ["16"] = UnitCategory.Medical,
        ["10"] = UnitCategory.Headquarters,
        ["11"] = UnitCategory.Headquarters,
        ["12"] = UnitCategory.Headquarters,
        ["13"] = UnitCategory.Headquarters,
        ["14"] = UnitCategory.Headquarters,
        ["17"] = UnitCategory.Headquarters,
    };

    public static UnitCategory? Of(string? typeCode)
        => typeCode is not null && ByTypeCode.TryGetValue(typeCode.Trim(), out var c) ? c : null;

    public static string Label(UnitCategory c) => c switch
    {
        UnitCategory.Insurance => "بیمه‌ای",
        UnitCategory.Medical => "درمانی",
        _ => "ستادی",
    };
}

/// <summary>دامنهٔ واحد گزارش: فقط واحد جاری، واحد و زیرمجموعه، یا همهٔ واحدها (نقش مدیریتی سطح کشور).</summary>
public enum ReportUnitScopeMode
{
    Self = 0,
    WithSubUnits = 1,
    AllUnits = 2,
}

/// <summary>گزارشی که می‌تواند روی چند واحد اجرا شود. <see cref="VahedScopeBehavior{TRequest,TResponse}"/> واحد جاری را می‌گذارد.</summary>
public interface IMultiUnitReportQuery : IVahedScopedQuery
{
    ReportUnitScopeMode UnitScope { get; }

    UnitCategory? UnitCategory { get; }
}

/// <summary>
/// فهرست واحدهای گزارش جاری (Scoped). null = فقط واحد جاری (رفتار قبلی). مخزن‌های گزارش وقتی پر است به‌جای
/// <c>VAHEDCODE = :vahed</c> از <c>IN (…)</c> استفاده می‌کنند — بدون تغییر امضای مخزن‌ها.
/// </summary>
public interface IReportUnitScope
{
    IReadOnlyList<string>? VahedCodes { get; }

    void Set(IReadOnlyList<string> vahedCodes);
}

public sealed class ReportUnitScope : IReportUnitScope
{
    public IReadOnlyList<string>? VahedCodes { get; private set; }

    public void Set(IReadOnlyList<string> vahedCodes) => VahedCodes = vahedCodes;
}

/// <summary>
/// پس از <see cref="Behaviors.VahedScopeBehavior{TRequest,TResponse}"/> اجرا می‌شود: برای
/// <see cref="IMultiUnitReportQuery"/> فهرست واحدها را می‌سازد. «همهٔ واحدها» فقط با نقش مدیریتی سطح کشور.
/// </summary>
public sealed class ReportUnitScopeBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private readonly IUnitAccessReadRepository _unitAccess;
    private readonly IReportUnitScope _scope;
    private readonly ICurrentUser _currentUser;
    private readonly IHeadquartersAccessService? _hq;

    public ReportUnitScopeBehavior(
        IUnitAccessReadRepository unitAccess, IReportUnitScope scope, ICurrentUser currentUser, IHeadquartersAccessService? hq = null)
    {
        _unitAccess = unitAccess;
        _scope = scope;
        _currentUser = currentUser;
        _hq = hq;
    }

    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        if (request is not IMultiUnitReportQuery q || (q.UnitScope == ReportUnitScopeMode.Self && q.UnitCategory is null))
            return await next(cancellationToken);

        // فاز ۵۴: «همهٔ واحدها» و انتخاب گروه واحد (بیمه‌ای/درمانی/ستادی) = قابلیت reports.unit-category
        // (پیش‌فرض: مدیر ستاد مرکزی یا نقش مدیریتی سطح کشور).
        if ((q.UnitScope == ReportUnitScopeMode.AllUnits || q.UnitCategory is not null) && !await CanSeeAllUnitsAsync(cancellationToken))
            throw new RoleAccessDeniedException(
                "گزارش «همهٔ واحدها» و انتخاب گروه واحد (بیمه‌ای/درمانی/ستادی) فقط برای کاربر ستاد مرکزی، نقش مدیریتی سطح کشور یا نقشی که این قابلیت را دارد ممکن است.");

        var all = await _unitAccess.GetAllUnitsAsync(cancellationToken);
        IEnumerable<UnitNode> units = q.UnitScope switch
        {
            ReportUnitScopeMode.AllUnits => all,
            ReportUnitScopeMode.WithSubUnits => Subtree(all, q.VahedCode),
            _ => all.Where(u => u.VahedCode == q.VahedCode),
        };
        if (q.UnitCategory is { } category)
            units = units.Where(u => UnitCategories.Of(u.TypeCode) == category);

        var codes = units.Select(u => u.VahedCode).Distinct().ToList();
        if (codes.Count == 0)
            throw new RoleAccessDeniedException("در این دامنه و گروه، واحدی برای گزارش نیست.");
        _scope.Set(codes);
        return await next(cancellationToken);
    }

    private async Task<bool> CanSeeAllUnitsAsync(CancellationToken cancellationToken)
    {
        if (_hq is not null)
            return await _hq.HasAbilityAsync(AbilityCatalog.ReportsUnitCategory, cancellationToken);
        if (_currentUser.IsInRole(AppRoles.National))
            return true;
        if (!_currentUser.IsInRole(AppRoles.SetadAdmin) || _currentUser.VahedCode is not { Length: > 0 } own)
            return false;
        return (await _unitAccess.GetUnitProfileAsync(own, cancellationToken))?.IsHeadquarters == true;
    }

    private static IEnumerable<UnitNode> Subtree(IReadOnlyList<UnitNode> all, string root)
    {
        var self = all.FirstOrDefault(u => u.VahedCode == root);
        if (self is null)
            return [];
        var byParent = all.Where(u => u.ParentId is not null).ToLookup(u => u.ParentId!.Value);
        var result = new List<UnitNode> { self };
        var visited = new HashSet<Guid> { self.Id };
        var queue = new Queue<Guid>([self.Id]);
        while (queue.Count > 0)
        {
            foreach (var child in byParent[queue.Dequeue()])
            {
                if (visited.Add(child.Id))
                {
                    result.Add(child);
                    queue.Enqueue(child.Id);
                }
            }
        }

        return result;
    }
}
