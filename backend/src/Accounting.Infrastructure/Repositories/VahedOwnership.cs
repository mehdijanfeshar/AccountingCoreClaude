using Accounting.Application.Common.Exceptions;

namespace Accounting.Infrastructure.Repositories;

/// <summary>
/// The single place record-level unit ownership is decided. Every by-<c>ID</c> repository method
/// for a table that has a <c>VAHEDCODE</c> column routes its answer through here, so the rule
/// lives once instead of 62 times.
///
/// <para>
/// <b>Why the repository and not the handler.</b> The reference project put this check in its
/// handlers and reached 12 handlers out of 372 — the gap that
/// <c>docs/centralaccount-business-reference.md</c> §21-1 records as the lesson not to repeat.
/// Here the caller's unit is a required parameter of the lookup itself, so a handler cannot fetch
/// a row without having stated whose it must be; forgetting is a compile error rather than a
/// silent hole. <c>VahedOwnershipConventionTests</c> additionally fails the build if a by-id
/// method on a unit-owned repository loses that parameter.
/// </para>
///
/// <para>
/// <b>Public, not internal, on purpose.</b> Nothing outside this assembly calls it, but this
/// project deliberately grants no <c>InternalsVisibleTo</c> to its test assemblies, and a rule
/// that decides who may read which record should be directly testable rather than only reachable
/// through a repository plus a SQLite fixture.
/// </para>
/// </summary>
public static class VahedOwnership
{
    /// <summary>
    /// Throws <see cref="UnitAccessDeniedException"/> when a row was found and belongs to another
    /// unit. Does nothing in the two cases that are not access violations:
    ///
    /// <list type="bullet">
    /// <item><description><paramref name="ownerVahedCode"/> is <see langword="null"/> because no
    /// row was found at all — callers pass <c>entity?.VAHEDCODE</c>, and "missing" is the
    /// handler's business (it raises <c>NotFoundException</c>), not an access decision.</description></item>
    /// <item><description><paramref name="ownerVahedCode"/> is blank, which marks a
    /// globally-shared row rather than an unowned one. The reference project stores
    /// <c>VahedCode = ""</c> for <c>Owner = Global</c> tafsili records; denying those would
    /// silently make every global record unreachable from every unit.</description></item>
    /// </list>
    ///
    /// Comparison is <see cref="StringComparison.Ordinal"/> on purpose. <c>VAHEDCODE</c> is a
    /// 4-character numeric-ish unit code, not human text, and a culture-sensitive comparison here
    /// would be both meaningless and a way for a locale to change who can read what.
    /// </summary>
    public static void EnsureOwned(
        string? ownerVahedCode,
        string callerVahedCode,
        Guid id,
        string resourceName)
    {
        if (string.IsNullOrWhiteSpace(ownerVahedCode))
        {
            return;
        }

        if (string.Equals(ownerVahedCode, callerVahedCode, StringComparison.Ordinal))
        {
            return;
        }

        throw new UnitAccessDeniedException(resourceName, id, ownerVahedCode, callerVahedCode);
    }
}
