using Accounting.Application.UnitAccess.Queries;

namespace Accounting.Application.Common.Interfaces;

/// <summary>
/// Resolves <b>which organizational units a given unit code may act as</b> — the single source of
/// truth for the «دسترسی به زیرمجموعه» rule, ported from the reference project's
/// <c>BusinessUserAccessRepository.HaveAccessToUnit</c> /
/// <c>VahedInfoRepository.GetAllVahedInfoByParentAsync</c>.
///
/// <para>
/// <b>Why this exists as its own repository.</b> Phases 19/32/33 built unit scoping as
/// <i>exact equality</i>: the caller's own <c>VAHEDCODE</c> came from the token and nothing else
/// was ever reachable. The «تغییر واحد» feature requires a caller to act as a unit that is not
/// their own, which means the system now needs a real answer to "which codes may this caller
/// legitimately choose". That answer is computed here, once, so that the eventual write-path
/// change has exactly one rule to consult rather than re-deriving a tree walk per call site —
/// the mistake <c>docs/centralaccount-business-reference.md</c> §21-1 records (12 of 372
/// reference handlers actually performed their check).
/// </para>
///
/// <para>
/// ⚠️ <b>This interface only answers the question. It does not enforce anything yet.</b> As of
/// phase 37-A the existing <c>VahedScopeBehavior</c> / <c>VahedOwnership</c> exact-equality rules
/// are untouched, so nothing here can widen access on its own.
/// </para>
/// </summary>
public interface IUnitAccessReadRepository
{
    /// <summary>
    /// Returns every unit <paramref name="vahedCode"/> may act as, ordered by <c>VAHEDCODE</c>.
    ///
    /// <list type="bullet">
    /// <item><description>A <b>headquarters</b> unit (see
    /// <c>UnitAccessReadRepository.HeadquartersVahedTypeCode</c>) gets every row in
    /// <c>TB_VAHED_INFO</c>.</description></item>
    /// <item><description>Any other unit gets <b>itself plus its whole descendant subtree</b>,
    /// walked over <c>PARENT_ID</c>.</description></item>
    /// <item><description>An unknown <paramref name="vahedCode"/> returns an <b>empty</b> list —
    /// never "everything". Failing closed matters because this value comes from a token claim and
    /// a mismatch against Legacy data must not silently grant access.</description></item>
    /// </list>
    ///
    /// <para>
    /// <b>Invariant worth relying on:</b> a known unit always appears in its own result (as the
    /// <c>IsDefault</c> row), so an empty list means exactly one thing — the code matched no
    /// <c>TB_VAHED_INFO</c> row. The caller translates that into a loud error; the reference
    /// project does the same with <c>UnitCodeNotFoundException</c>
    /// («کد واحد با این مشخصات یافت نشد») rather than returning an empty page.
    /// </para>
    /// </summary>
    Task<IReadOnlyList<AccessibleUnitDto>> GetAccessibleUnitsAsync(
        string vahedCode,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns the display name and headquarters flag for a single unit code, or
    /// <see langword="null"/> when no <c>TB_VAHED_INFO</c> row carries that code.
    ///
    /// Separate from <see cref="GetAccessibleUnitsAsync"/> so that describing the caller — which
    /// every page load does — costs one indexed lookup instead of materialising and walking the
    /// whole unit tree.
    /// </summary>
    Task<UnitProfile?> GetUnitProfileAsync(string vahedCode, CancellationToken cancellationToken = default);

    /// <summary>
    /// True when <paramref name="ownVahedCode"/> may act as <paramref name="targetVahedCode"/> —
    /// the membership test behind <see cref="IUnitScopeResolver"/>, and the reference project's
    /// <c>BusinessUserAccess.HaveAccessToUnit</c>.
    ///
    /// <para>
    /// Deliberately <b>not</b> implemented as "build the full accessible set, then check
    /// <c>Contains</c>". That would materialise the whole unit table on every single request that
    /// carries a unit header. This answers the narrower question directly, which on real data
    /// (1046 units, tree depth 2) is a couple of indexed lookups.
    /// </para>
    ///
    /// <para>
    /// Returns <see langword="false"/> — never throws — when either code is unknown. "I cannot
    /// confirm you may act as this" and "you may not" get the same answer on purpose: the caller
    /// turns that into a 403, and no variant of the answer should leak whether a unit code exists.
    /// </para>
    /// </summary>
    Task<bool> CanActAsAsync(
        string ownVahedCode,
        string targetVahedCode,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// The two facts about the caller's own unit that <c>GET /api/me</c> needs.
/// </summary>
/// <param name="VahedName">TB_VAHED_INFO.VAHEDNAME.</param>
/// <param name="IsHeadquarters">True when this unit's type grants blanket access to every unit.</param>
public sealed record UnitProfile(string VahedName, bool IsHeadquarters);
