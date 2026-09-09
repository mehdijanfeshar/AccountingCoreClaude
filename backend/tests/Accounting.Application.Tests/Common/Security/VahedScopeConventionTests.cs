using System.Reflection;
using System.Text.Json.Serialization;
using Accounting.Application.Common;
using Accounting.Application.Common.Security;
using MediatR;

namespace Accounting.Application.Tests.Common.Security;

/// <summary>
/// Reflection-based convention guard for IDOR risk #1 (CLAUDE.md "ریسک‌های باز"). This is the
/// project-wide backstop for the exact failure mode recorded in
/// <c>docs/centralaccount-business-reference.md</c> (فاز ۱۷): the reference project
/// <c>D:\CentralAccount</c> tried to enforce <c>VAHEDCODE</c> scoping per-handler and only 12 of
/// 372 handlers actually did it. <see cref="Behaviors.VahedScopeBehavior{TRequest,TResponse}"/>
/// already makes the *runtime* enforcement automatic for anything that opts in via
/// <see cref="IVahedScoped"/> — but nothing forced a future Create/Update command or list Query to
/// opt in. This file is that force: it scans the whole <c>Accounting.Application</c> assembly by
/// naming convention and fails loudly for any type that looks like it should be scoped but isn't,
/// unless it is named in one of the two exemption groups below.
///
/// <b>Exemption groups — read this before adding a name to either set below.</b>
/// <list type="bullet">
/// <item><description><b>Group A — the underlying Legacy table has no <c>VAHEDCODE</c> column at
/// all</b> (verified against <c>backend/src/Accounting.Domain/Entity/</c> for all 8 entities:
/// <c>TB_ACCOUNTCODE_INTERFACE</c>, <c>TB_ACCOUNTEXCEPTION</c>, <c>TB_ACCOUNTCODE</c>,
/// <c>TB_LEVEL_TAFSIL</c>, <c>TB_TAFSIL_GROUP</c>, <c>TB_RABET</c>, <c>TB_WHITELIST</c>,
/// <c>TB_WHITEANDBLACKLIST</c>). There is nothing to scope — implementing
/// <see cref="IVahedScoped"/> would require inventing a column that does not exist. This group is
/// permanent; it only changes if one of these tables ever gains a <c>VAHEDCODE</c> column.</description></item>
/// <item><description><b>Group B — exempted by explicit project-owner decision</b> (2 entities):
/// <list type="bullet">
/// <item><description><c>VahedInfo</c> (<c>TB_VAHED_INFO</c>) — this table IS the organizational
/// unit reference table itself. Forcing Create to overwrite <c>VahedCode</c> with the caller's own
/// unit would make it impossible to ever create a NEW unit (and would immediately collide with the
/// UNIQUE constraint <c>UK_VAHEDINFO</c> the moment a second row was attempted); forcing the list
/// Query to filter by the caller's unit would make it impossible for any UI to populate a unit
/// picker or resolve <c>ParentId</c>. Permanent admin/global-table exemption.</description></item>
/// <item><description><c>PersonAction</c> (<c>TB_PERSON_ACTION</c>) — ⚠️ <b>TEMPORARY, pending a
/// project-owner decision.</b> The owner wants this entity scoped by <c>VahedType</c> rather than
/// <c>VahedCode</c>, but <c>TB_PERSON_ACTION</c> has been verified (both the Domain entity and the
/// Fluent mapping in <c>LegacyDbContext.cs</c>) to have NO <c>VAHEDTYPE_ID</c> column at all. There
/// is currently no column to scope by, of either kind, so this is left unscoped rather than
/// guessing a mechanism. <b>Whoever resolves that open decision must revisit this exemption</b> —
/// either remove it (if a scoping column is added) or replace it with a durable comment explaining
/// why <c>PersonAction</c> stays permanently unscoped.</description></item>
/// </list></description></item>
/// </list>
///
/// If a future entity gains an actual <c>VAHEDCODE</c> column and its Create/Update
/// command/list-Query is NOT added to either group and does NOT implement <see cref="IVahedScoped"/>,
/// this file must fail — that is precisely the scenario it exists to catch.
/// </summary>
public sealed class VahedScopeConventionTests
{
    private static readonly Assembly ApplicationAssembly = typeof(IVahedScoped).Assembly;

    /// <summary>
    /// Group A — entities whose Legacy table has no <c>VAHEDCODE</c> column at all. See this
    /// class's XML doc for the full rationale and the verification method.
    /// </summary>
    private static readonly HashSet<string> NoVahedCodeColumnExemptCommands = new(StringComparer.Ordinal)
    {
        // TB_ACCOUNTCODE_INTERFACE
        "CreateAccountCodeInterfaceCommand",
        "UpdateAccountCodeInterfaceCommand",
        // TB_ACCOUNTEXCEPTION
        "CreateAccountExceptionCommand",
        "UpdateAccountExceptionCommand",
        // TB_ACCOUNTCODE
        "CreateAccountCodeCommand",
        "UpdateAccountCodeCommand",
        // TB_LEVEL_TAFSIL
        "CreateLevelTafsilCommand",
        "UpdateLevelTafsilCommand",
        // TB_TAFSIL_GROUP
        "CreateTafsilGroupCommand",
        "UpdateTafsilGroupCommand",
        // TB_RABET
        "CreateRabetCommand",
        "UpdateRabetCommand",
        // TB_WHITELIST
        "CreateWhiteListCommand",
        "UpdateWhiteListCommand",
        // TB_WHITEANDBLACKLIST
        "CreateWhiteAndBlackListCommand",
        "UpdateWhiteAndBlackListCommand",
    };

    /// <summary>
    /// Group B — exempted by explicit, named project-owner decision. See this class's XML doc for
    /// the full rationale, including which half of this set is TEMPORARY.
    /// </summary>
    private static readonly HashSet<string> ProjectOwnerDecisionExemptCommands = new(StringComparer.Ordinal)
    {
        // TB_VAHED_INFO — permanent admin/global-table exemption.
        "CreateVahedInfoCommand",
        "UpdateVahedInfoCommand",
        // TB_PERSON_ACTION — TEMPORARY, pending project-owner decision (no VAHEDTYPE_ID column exists).
        "CreatePersonActionCommand",
        "UpdatePersonActionCommand",
    };

    /// <summary>Group A, applied to list Queries instead of commands (same 8 entities).</summary>
    private static readonly HashSet<string> NoVahedCodeColumnExemptQueries = new(StringComparer.Ordinal)
    {
        "GetAccountCodeInterfacesQuery",
        "GetAccountExceptionsQuery",
        "GetAccountCodesQuery",
        "GetLevelTafsilsQuery",
        "GetTafsilGroupsQuery",
        "GetRabetsQuery",
        "GetWhiteListsQuery",
        "GetWhiteAndBlackListsQuery",
    };

    /// <summary>Group B, applied to list Queries instead of commands (same 2 entities).</summary>
    private static readonly HashSet<string> ProjectOwnerDecisionExemptQueries = new(StringComparer.Ordinal)
    {
        "GetVahedInfosQuery",
        "GetPersonActionsQuery",
    };

    private static IEnumerable<Type> ConcreteApplicationTypes =>
        ApplicationAssembly.GetTypes().Where(t => t is { IsClass: true, IsAbstract: false });

    private static bool ImplementsInterface(Type type, Type interfaceType) =>
        interfaceType.IsAssignableFrom(type);

    /// <summary>
    /// True if <paramref name="type"/> implements <c>IRequest&lt;PagedResult&lt;T&gt;&gt;</c> for
    /// some <c>T</c> — the shape of every list/paged Query in this project.
    /// </summary>
    private static bool ReturnsPagedResult(Type type) =>
        type.GetInterfaces().Any(i =>
            i.IsGenericType &&
            i.GetGenericTypeDefinition() == typeof(IRequest<>) &&
            i.GetGenericArguments()[0] is { IsGenericType: true } responseType &&
            responseType.GetGenericTypeDefinition() == typeof(PagedResult<>));

    [Fact]
    public void EveryCreateOrUpdateCommand_IsEitherVahedScoped_OrExplicitlyExempt()
    {
        var candidates = ConcreteApplicationTypes
            .Where(t => t.Name.EndsWith("Command", StringComparison.Ordinal))
            .Where(t => t.Name.StartsWith("Create", StringComparison.Ordinal)
                        || t.Name.StartsWith("Update", StringComparison.Ordinal))
            .ToList();

        var offenders = candidates
            .Where(t => !ImplementsInterface(t, typeof(IVahedScopedCommand)))
            .Where(t => !NoVahedCodeColumnExemptCommands.Contains(t.Name))
            .Where(t => !ProjectOwnerDecisionExemptCommands.Contains(t.Name))
            .Select(t => t.FullName ?? t.Name)
            .OrderBy(name => name, StringComparer.Ordinal)
            .ToList();

        Assert.True(
            offenders.Count == 0,
            "The following Create/Update command(s) implement neither IVahedScopedCommand nor "
                + "appear in an exemption list in VahedScopeConventionTests: "
                + string.Join(", ", offenders)
                + ". For each one, either (a) make it implement IVahedScopedCommand so "
                + "VahedScopeBehavior server-assigns VahedCode, or (b) if its underlying Legacy "
                + "table genuinely has no VAHEDCODE column, add its type name to "
                + $"{nameof(NoVahedCodeColumnExemptCommands)}, or (c) if the project owner made an "
                + $"explicit decision to exempt it, add it to {nameof(ProjectOwnerDecisionExemptCommands)} "
                + "with a comment recording that decision. Do not silently ignore this failure.");

        // Sanity check: the scan must not be vacuous — the assembly genuinely has Create/Update
        // commands for this guard to mean anything.
        Assert.NotEmpty(candidates);
    }

    [Fact]
    public void EveryListQuery_IsEitherVahedScoped_OrExplicitlyExempt()
    {
        var candidates = ConcreteApplicationTypes
            .Where(t => t.Name.EndsWith("Query", StringComparison.Ordinal))
            .Where(ReturnsPagedResult)
            .ToList();

        var offenders = candidates
            .Where(t => !ImplementsInterface(t, typeof(IVahedScopedQuery)))
            .Where(t => !NoVahedCodeColumnExemptQueries.Contains(t.Name))
            .Where(t => !ProjectOwnerDecisionExemptQueries.Contains(t.Name))
            .Select(t => t.FullName ?? t.Name)
            .OrderBy(name => name, StringComparer.Ordinal)
            .ToList();

        Assert.True(
            offenders.Count == 0,
            "The following list Query/Queries (IRequest<PagedResult<T>>) implement neither "
                + "IVahedScopedQuery nor appear in an exemption list in VahedScopeConventionTests: "
                + string.Join(", ", offenders)
                + ". For each one, either (a) make it implement IVahedScopedQuery so "
                + "VahedScopeBehavior server-assigns VahedCode and the repository filters by it, "
                + "or (b) if its underlying Legacy table genuinely has no VAHEDCODE column, add its "
                + $"type name to {nameof(NoVahedCodeColumnExemptQueries)}, or (c) if the project "
                + "owner made an explicit decision to exempt it, add it to "
                + $"{nameof(ProjectOwnerDecisionExemptQueries)} with a comment recording that decision. "
                + "Do not silently ignore this failure.");

        // Sanity check: the scan must not be vacuous.
        Assert.NotEmpty(candidates);
    }

    /// <summary>
    /// Deliberately OUT of scope, by explicit project-owner decision: single-record
    /// <c>Get...ByIdQuery</c> lookups are never <see cref="IVahedScoped"/> — record-ownership
    /// verification on direct-by-id access is a separate, not-yet-closed IDOR gap (see
    /// <c>UpdateWorkShopCommand</c>'s XML doc "Scope note" and CLAUDE.md risk #1), not something
    /// this convention test should silently paper over by requiring the marker. If a
    /// <c>Get...ByIdQuery</c> ever DOES gain the marker, that is itself a change worth a second
    /// look (does the repository now filter by unit for a by-id lookup? was that intentional?) —
    /// so this test fails loudly in that direction too, rather than just ignoring by-id queries
    /// entirely.
    /// </summary>
    [Fact]
    public void NoGetByIdQuery_ImplementsIVahedScoped()
    {
        var byIdQueries = ConcreteApplicationTypes
            .Where(t => t.Name.EndsWith("ByIdQuery", StringComparison.Ordinal))
            .ToList();

        var offenders = byIdQueries
            .Where(t => ImplementsInterface(t, typeof(IVahedScoped)))
            .Select(t => t.FullName ?? t.Name)
            .OrderBy(name => name, StringComparer.Ordinal)
            .ToList();

        Assert.True(
            offenders.Count == 0,
            "The following Get...ByIdQuery type(s) unexpectedly implement IVahedScoped, which is "
                + "deliberately out of scope for by-id lookups (project-owner decision): "
                + string.Join(", ", offenders)
                + ". If this is an intentional new decision to scope by-id access, update this "
                + "test (and its XML doc) to reflect that decision explicitly rather than deleting "
                + "the assertion.");

        // Sanity check: the scan must not be vacuous.
        Assert.NotEmpty(byIdQueries);
    }

    [Fact]
    public void EveryVahedScopedType_HasJsonIgnoredVahedCode_ThatIsNotACallerSuppliablePositionalParameter()
    {
        var vahedScopedTypes = ConcreteApplicationTypes
            .Where(t => ImplementsInterface(t, typeof(IVahedScoped)))
            .ToList();

        var missingJsonIgnore = new List<string>();
        var callerSuppliableViaConstructor = new List<string>();

        foreach (var type in vahedScopedTypes)
        {
            var vahedCodeProperty = type.GetProperty("VahedCode", BindingFlags.Public | BindingFlags.Instance);
            if (vahedCodeProperty is null)
            {
                // IVahedScoped guarantees this property exists on the interface; a missing
                // reflected property here would indicate an explicit interface implementation,
                // which none of the current types use. Treat as its own failure category.
                missingJsonIgnore.Add($"{type.FullName} (VahedCode property not found via reflection)");
                continue;
            }

            var hasJsonIgnore = vahedCodeProperty.GetCustomAttribute<JsonIgnoreAttribute>() is not null;
            if (!hasJsonIgnore)
            {
                missingJsonIgnore.Add(type.FullName ?? type.Name);
            }

            // The primary (positional record) constructor is the one whose parameter count
            // matches the number of init-time-settable properties declared by the record; for
            // every type in this project it is simply the constructor with the most parameters.
            var primaryConstructor = type.GetConstructors()
                .OrderByDescending(c => c.GetParameters().Length)
                .FirstOrDefault();

            var hasVahedCodeConstructorParameter = primaryConstructor?.GetParameters()
                .Any(p => string.Equals(p.Name, "VahedCode", StringComparison.Ordinal)) ?? false;

            if (hasVahedCodeConstructorParameter)
            {
                callerSuppliableViaConstructor.Add(type.FullName ?? type.Name);
            }
        }

        Assert.True(
            missingJsonIgnore.Count == 0,
            "The following IVahedScoped type(s) do not have [JsonIgnore] on their VahedCode "
                + "property, so a caller-supplied value in the request body would be model-bound "
                + "before VahedScopeBehavior overwrites it (and the field would leak into the "
                + "Swagger schema): "
                + string.Join(", ", missingJsonIgnore)
                + ". Add [JsonIgnore] (System.Text.Json.Serialization) to the VahedCode property.");

        Assert.True(
            callerSuppliableViaConstructor.Count == 0,
            "The following IVahedScoped type(s) accept VahedCode as a positional record "
                + "constructor parameter, meaning a caller could set it directly via `new(...)` "
                + "(and, more importantly, it signals VahedCode is meant to be caller input rather "
                + "than server-assigned): "
                + string.Join(", ", callerSuppliableViaConstructor)
                + ". VahedCode must be a plain mutable property assigned only by "
                + "VahedScopeBehavior, never a constructor parameter — see IVahedScoped's XML doc "
                + "for why it must stay a settable property.");

        // Sanity check: the scan must not be vacuous.
        Assert.NotEmpty(vahedScopedTypes);
    }

    [Fact]
    public void NoDeleteCommand_ImplementsIVahedScoped()
    {
        var deleteCommands = ConcreteApplicationTypes
            .Where(t => t.Name.EndsWith("Command", StringComparison.Ordinal))
            .Where(t => t.Name.StartsWith("Delete", StringComparison.Ordinal))
            .ToList();

        var offenders = deleteCommands
            .Where(t => ImplementsInterface(t, typeof(IVahedScoped)))
            .Select(t => t.FullName ?? t.Name)
            .OrderBy(name => name, StringComparer.Ordinal)
            .ToList();

        Assert.True(
            offenders.Count == 0,
            "The following Delete command(s) unexpectedly implement IVahedScoped, which is "
                + "deliberately out of scope for Delete (soft-delete does not touch VAHEDCODE): "
                + string.Join(", ", offenders)
                + ". If this is an intentional new decision to scope Delete commands, update this "
                + "test to reflect that decision explicitly rather than deleting the assertion.");

        // Sanity check: the scan must not be vacuous.
        Assert.NotEmpty(deleteCommands);
    }
}
