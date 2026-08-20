using System.Reflection;
using Accounting.Application.Common.Interfaces;
using Accounting.Domain.Entity;
using MediatR;

namespace Accounting.Application.Tests.Common;

/// <summary>
/// Locks in the team rule confirmed alongside the 2026-08-20 project-owner decision (recorded in
/// <c>docs/tamin-core-entity-reference.md</c> بخش ۵): every <c>*_LINK_TAFSIL*</c>/<c>*_LINK_LEVEL*</c>
/// Legacy table is permanently embedded — it is only ever mutated as a side effect of its parent
/// aggregate's own write path (e.g. <c>TB_VOUCHERDETAIL_LINK_TAFSILI</c> only via
/// <see cref="IVoucherDetailRepository.SoftDeleteTafsiliLinksAsync"/> /
/// <see cref="IVoucherHeadRepository.SoftDeleteDetailTreeAsync"/>) — and must never get its own
/// independent Create/Read/Update/Delete use case. A regression here would mean somebody started
/// building a standalone CRUD surface for a table the project owner explicitly decided must stay
/// embedded forever.
/// </summary>
public sealed class NoIndependentLinkTableWritePathTests
{
    private static readonly Type[] RepositoryInterfacesUnderTest =
    {
        typeof(IVoucherDetailRepository),
        typeof(IVoucherHeadRepository),
    };

    private static bool IsLinkTableEntityType(Type type)
    {
        var name = type.Name;
        return name.Contains("_LINK_TAFSIL", StringComparison.OrdinalIgnoreCase)
            || name.Contains("_LINK_LEVEL", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Unwraps <c>Task</c>/<c>Task&lt;T&gt;</c> to the underlying <c>T</c> (or <see langword="null"/>
    /// for a non-generic <c>Task</c>), so both the return type of an <c>AddAsync</c>-style method
    /// and of a <c>GetForUpdateAsync</c>-style method can be inspected uniformly.
    /// </summary>
    private static Type? UnwrapTaskResultType(Type returnType)
    {
        if (returnType == typeof(Task))
        {
            return null;
        }

        if (returnType.IsGenericType && returnType.GetGenericTypeDefinition() == typeof(Task<>))
        {
            var resultType = returnType.GetGenericArguments()[0];

            // GetForUpdateAsync-style methods return T? (nullable reference) — Nullable<T> only
            // applies to value types, so for a reference type the unwrapped type IS the nullable
            // annotation's underlying type already; nothing further to strip.
            return resultType;
        }

        return null;
    }

    [Fact]
    public void RepositoryInterfaces_ExposeNoAddAsyncOrGetForUpdateAsyncForAnyLinkTableEntity()
    {
        var offendingMethods = new List<string>();

        foreach (var repositoryInterface in RepositoryInterfacesUnderTest)
        {
            foreach (var method in repositoryInterface.GetMethods())
            {
                var isAdd = method.Name.Equals("AddAsync", StringComparison.Ordinal);
                var isGetForUpdate = method.Name.Equals("GetForUpdateAsync", StringComparison.Ordinal);

                if (!isAdd && !isGetForUpdate)
                {
                    continue;
                }

                if (isAdd)
                {
                    var entityParameter = method.GetParameters().FirstOrDefault();
                    if (entityParameter is not null && IsLinkTableEntityType(entityParameter.ParameterType))
                    {
                        offendingMethods.Add($"{repositoryInterface.Name}.{method.Name}({entityParameter.ParameterType.Name})");
                    }
                }

                if (isGetForUpdate)
                {
                    var resultType = UnwrapTaskResultType(method.ReturnType);
                    if (resultType is not null && IsLinkTableEntityType(resultType))
                    {
                        offendingMethods.Add($"{repositoryInterface.Name}.{method.Name}() -> {resultType.Name}");
                    }
                }
            }
        }

        Assert.True(
            offendingMethods.Count == 0,
            "Found independent AddAsync/GetForUpdateAsync write path(s) for a *_LINK_TAFSIL*/*_LINK_LEVEL* "
                + $"entity: {string.Join(", ", offendingMethods)}. Every such table must stay embedded — "
                + "mutate it only via its parent aggregate's own cascade method (e.g. SoftDeleteTafsiliLinksAsync).");
    }

    /// <summary>
    /// Sanity check that the reflection-based type matcher above actually recognises the one
    /// link-table entity currently in the model — guards against the assertion above silently
    /// passing because the name pattern stopped matching anything.
    /// </summary>
    [Fact]
    public void IsLinkTableEntityType_RecognisesKnownLinkEntity()
    {
        Assert.True(IsLinkTableEntityType(typeof(TB_VOUCHERDETAIL_LINK_TAFSILI)));
        Assert.False(IsLinkTableEntityType(typeof(TB_VOUCHERSDETAIL)));
        Assert.False(IsLinkTableEntityType(typeof(TB_VOUCHERSHEAD)));
    }

    [Fact]
    public void NoMediatRRequestType_InApplicationAssembly_IsNamedForALinkTafsiliOrLinkLevelUseCase()
    {
        var applicationAssembly = typeof(IVoucherDetailRepository).Assembly;

        var requestInterfaceTypes = new[] { typeof(IRequest), typeof(IBaseRequest) };

        var requestTypes = applicationAssembly.GetTypes()
            .Where(t => t is { IsClass: true, IsAbstract: false } || t.IsValueType && !t.IsPrimitive)
            .Where(t => t.GetInterfaces().Any(i =>
                requestInterfaceTypes.Contains(i) ||
                (i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IRequest<>))))
            .ToList();

        var offendingTypes = requestTypes
            .Where(t =>
                t.Name.Contains("LinkTafsili", StringComparison.OrdinalIgnoreCase) ||
                t.Name.Contains("LinkLevel", StringComparison.OrdinalIgnoreCase))
            .Select(t => t.FullName)
            .ToList();

        Assert.True(
            offendingTypes.Count == 0,
            $"Found MediatR request type(s) that look like an independent Link table use case: {string.Join(", ", offendingTypes)}. "
                + "*_LINK_TAFSIL*/*_LINK_LEVEL* tables must stay embedded and never get their own Command/Query.");

        // Sanity check the scan itself is not vacuous — the Application assembly must genuinely
        // contain MediatR request types for this guard to mean anything.
        Assert.NotEmpty(requestTypes);
    }
}
