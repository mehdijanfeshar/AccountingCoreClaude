using Accounting.Application.Common.Interfaces;
using Accounting.Application.FinancialStatements;

namespace Accounting.Application.Tests.FinancialStatements;

/// <summary>
/// درخت واحد نمونه برای تست‌های صورت‌های مالی:
/// <code>
/// 1 (ستاد)
/// ├─ 10 ── 101, 102
/// └─ 20 ── 201
/// </code>
/// </summary>
internal static class FsTestData
{
    public static readonly Guid HqId = Guid.NewGuid();
    public static readonly Guid ProvId = Guid.NewGuid();
    public static readonly Guid U101Id = Guid.NewGuid();
    public static readonly Guid U102Id = Guid.NewGuid();
    public static readonly Guid OtherId = Guid.NewGuid();
    public static readonly Guid U201Id = Guid.NewGuid();

    public static List<UnitNode> Units() =>
    [
        new(HqId, "1", "ستاد", null, true),
        new(ProvId, "10", "استان", HqId, false, "P"),
        new(U101Id, "101", "واحد ۱۰۱", ProvId, false),
        new(U102Id, "102", "واحد ۱۰۲", ProvId, false),
        new(OtherId, "20", "استان دیگر", HqId, false),
        new(U201Id, "201", "واحد ۲۰۱", OtherId, false),
    ];

    public static FsUnitScope HqScope() =>
        FsUnitScope.Build("1", Units(), new HashSet<string> { "1", "10", "101", "102", "20", "201" });

    public static FsUnitScope ProvinceScope() =>
        FsUnitScope.Build("10", Units(), new HashSet<string> { "10", "101", "102" });

    public static FsUnitScope LeafScope() =>
        FsUnitScope.Build("101", Units(), new HashSet<string> { "101" });
}
