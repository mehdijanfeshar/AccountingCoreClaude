using Accounting.Application.Common.Exceptions;
using Accounting.Application.Common.Interfaces;

namespace Accounting.Application.FinancialStatements;

/// <summary>
/// موقعیت یک واحد در درخت واحدها، برای قواعد تفکیک واحد ماژول صورت‌های مالی (تصمیم صاحب پروژه
/// ۲۰۲۶-۰۹-۳۰، <c>docs/fs-module.md</c> §۸):
/// <list type="bullet">
/// <item><b>دیدن قالب:</b> مشترک، یا مال خود/اجداد (قالب شرکت برای زیرمجموعه‌هایش)، یا مال زیرمجموعه.</item>
/// <item><b>تغییر قالب:</b> مشترک فقط ستاد؛ اختصاصی = خود یا زیرمجموعه (ستاد = همه).</item>
/// <item><b>اولویت در اجرا:</b> قالب نزدیک‌ترین جد (از خود به بالا)، بعد مشترک.</item>
/// <item><b>گروه سهم واحدها:</b> زیرواحد سطح اولِ زیر این واحد که هر واحد زیرمجموعه داخلش است.</item>
/// </list>
/// «زیرمجموعه» همان تعریف <c>IUnitAccessReadRepository.GetAccessibleUnitsAsync</c> است (ستاد = همهٔ واحدها).
/// </summary>
public sealed class FsUnitScope
{
    private readonly Dictionary<string, string> _groupByUnit;

    private FsUnitScope(
        string vahedCode,
        bool isHeadquarters,
        IReadOnlyList<string> ancestorsOrSelf,
        IReadOnlySet<string> accessible,
        Dictionary<string, string> groupByUnit,
        IReadOnlyDictionary<string, string> names)
    {
        VahedCode = vahedCode;
        IsHeadquarters = isHeadquarters;
        AncestorsOrSelf = ancestorsOrSelf;
        Accessible = accessible;
        _groupByUnit = groupByUnit;
        Names = names;
    }

    public string VahedCode { get; }

    public bool IsHeadquarters { get; }

    /// <summary>خود واحد و اجدادش، نزدیک‌ترین اول.</summary>
    public IReadOnlyList<string> AncestorsOrSelf { get; }

    /// <summary>خود و همهٔ زیرمجموعه‌ها (ستاد = همهٔ واحدها).</summary>
    public IReadOnlySet<string> Accessible { get; }

    /// <summary>نام همهٔ واحدها بر اساس کد.</summary>
    public IReadOnlyDictionary<string, string> Names { get; }

    public bool CanSee(string? ownerVahedCode)
        => ownerVahedCode is null || AncestorsOrSelf.Contains(ownerVahedCode) || Accessible.Contains(ownerVahedCode);

    public bool CanEdit(string? ownerVahedCode)
        => ownerVahedCode is null ? IsHeadquarters : Accessible.Contains(ownerVahedCode);

    /// <summary>رتبهٔ مالک برای اولویت اجرا: ۰ = خود واحد، ۱ = والد، …؛ مشترک = بزرگ‌ترین؛ غیرمرتبط = null.</summary>
    public int? PriorityOf(string? ownerVahedCode)
    {
        if (ownerVahedCode is null)
        {
            return int.MaxValue;
        }

        var index = AncestorsOrSelf.ToList().IndexOf(ownerVahedCode);
        return index < 0 ? null : index;
    }

    /// <summary>زیرواحد سطح اولِ زیر این واحد که <paramref name="unitCode"/> داخلش است؛ خود واحد برای اسناد خودش.</summary>
    public string GroupOf(string unitCode) => _groupByUnit.GetValueOrDefault(unitCode, VahedCode);

    public void EnsureCanEdit(string? ownerVahedCode)
    {
        if (!CanEdit(ownerVahedCode))
        {
            throw new FsAccessDeniedException(ownerVahedCode is null
                ? "قالب مشترک را فقط ستاد مرکزی می‌تواند تغییر دهد. برای این واحد یک قالب اختصاصی بسازید."
                : "این قالب مال واحد دیگری است و از این واحد قابل تغییر نیست.");
        }
    }

    public static FsUnitScope Build(string vahedCode, IReadOnlyList<UnitNode> units, IReadOnlySet<string> accessible)
    {
        var byId = units.ToDictionary(u => u.Id);
        var self = units.FirstOrDefault(u => u.VahedCode == vahedCode);

        var ancestors = new List<string> { vahedCode };

        if (self is not null)
        {
            var seen = new HashSet<Guid> { self.Id };

            for (var p = self.ParentId; p is { } pid && byId.TryGetValue(pid, out var parent) && seen.Add(pid) && ancestors.Count < 50; p = parent.ParentId)
            {
                ancestors.Add(parent.VahedCode);
            }
        }

        // گروه هر واحدِ در دسترس: از خودش بالا برو تا به فرزند مستقیم واحد اجرا برسی. واحدی که در
        // زنجیره‌اش به این واحد نمی‌رسد (فقط برای ستاد با دسترسی سراسری ممکن است) گروه خودِ ریشه‌اش است.
        var groups = new Dictionary<string, string>(StringComparer.Ordinal);

        foreach (var code in accessible)
        {
            var node = units.FirstOrDefault(u => u.VahedCode == code);

            if (node is null || code == vahedCode)
            {
                continue;
            }

            var current = node;
            var guard = 0;

            while (current.ParentId is { } pid && byId.TryGetValue(pid, out var parent) && parent.VahedCode != vahedCode && guard++ < 50)
            {
                current = parent;
            }

            groups[code] = current.VahedCode;
        }

        return new FsUnitScope(
            vahedCode,
            self?.IsHeadquarters ?? false,
            ancestors,
            accessible,
            groups,
            units.GroupBy(u => u.VahedCode).ToDictionary(g => g.Key, g => g.First().VahedName, StringComparer.Ordinal));
    }
}

public interface IFsUnitScopeProvider
{
    Task<FsUnitScope> GetAsync(string vahedCode, CancellationToken cancellationToken = default);
}

public sealed class FsUnitScopeProvider : IFsUnitScopeProvider
{
    private readonly IUnitAccessReadRepository _unitAccess;

    public FsUnitScopeProvider(IUnitAccessReadRepository unitAccess)
    {
        _unitAccess = unitAccess;
    }

    public async Task<FsUnitScope> GetAsync(string vahedCode, CancellationToken cancellationToken = default)
    {
        var units = await _unitAccess.GetAllUnitsAsync(cancellationToken);
        var accessible = (await _unitAccess.GetAccessibleUnitsAsync(vahedCode, cancellationToken))
            .Select(u => u.VahedCode)
            .ToHashSet(StringComparer.Ordinal);

        accessible.Add(vahedCode);
        return FsUnitScope.Build(vahedCode, units, accessible);
    }
}
