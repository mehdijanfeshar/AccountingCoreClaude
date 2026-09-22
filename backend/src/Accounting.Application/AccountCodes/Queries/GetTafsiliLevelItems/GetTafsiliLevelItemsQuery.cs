using System.Text.Json.Serialization;
using Accounting.Application.Common;
using Accounting.Application.Common.Security;
using MediatR;

namespace Accounting.Application.AccountCodes.Queries.GetTafsiliLevelItems;

/// <summary>
/// Returns a page of selectable تفصیلی items for one (معین, level) pair, via the domain chain
/// <c>TB_ACCOUNTCODE → TB_ACCOUNT_LINK_TAFSILGROUP → TB_TAFSIL_LINK_TAFSILGROUP → TB_TAFSILI</c>.
/// Backs <c>GET /api/account-codes/{accountCodeId}/tafsili-levels/{levelId}/items</c> (phase 20-b).
///
/// <b>Rule B — visibility scoping</b> (verified directly against the reference project's source,
/// which runs on the same Oracle schema:
/// <c>D:\CentralAccount\Infrastructure.Persistance.EF\Repositories\TbTafsilLinkTafsilGroupRepository.cs:115-120</c>
/// and the raw-SQL twin in <c>TbAccountLinkTafsilGroupRepository.cs:173-174</c>). A
/// <c>TB_TAFSIL_LINK_TAFSILGROUP</c> row is visible to this caller iff, verbatim:
/// <c>link.VAHEDCODE == callerVahedCode || link.VAHEDTYPE == 3 (All) || link.VAHEDTYPE ==
/// callerCategory</c>. A <c>VAHEDTYPE</c> of <see langword="null"/> is therefore NOT visible
/// unless <c>VAHEDCODE</c> matches — do not "helpfully" add <c>|| VAHEDTYPE == null</c>.
/// <c>callerCategory</c> is derived from <see cref="VahedCode"/> via
/// <c>TB_VAHED_INFO → TB_VAHED_TYPE.TYPECODE</c> and
/// <see cref="Accounting.Domain.ValueObjects.VahedCategoryMapper.FromTypeCode"/> — see
/// <c>Accounting.Infrastructure.Repositories.TafsiliLookupReadRepository</c> for the exact query
/// and the (deliberate) fallback-to-All divergence from the reference project when the caller's
/// unit can't be found.
///
/// <b>IS <see cref="IVahedScopedQuery"/></b> — unlike <c>GetTafsiliLevelsQuery</c>, Rule B needs
/// the caller's own unit code, so <see cref="VahedCode"/> is server-assigned by
/// <c>VahedScopeBehavior</c> exactly like every other scoped query, and this endpoint also
/// declares 403.
///
/// <b><see cref="Search"/> heuristic</b> — copied verbatim from the reference project's own
/// (<c>TbAccountLinkTafsilGroupRepository.cs:186-190</c>), which is surprising enough that a
/// future reader might otherwise "fix" it into an OR: a non-blank term containing any digit
/// matches <c>TAFSILI_CODE LIKE %term%</c>; otherwise it matches <c>TAFSILI_NAME LIKE %term%</c>.
/// Never both. A blank/whitespace-only <see cref="Search"/> means "no filter".
///
/// Deliberately excludes <c>TB_TAFSILI.ISACTIVE</c> from every filter. As of phase 27 batch 1,
/// <c>ISACTIVE</c> is correctly typed as <see cref="Accounting.Domain.ValueObjects.TafsiliActiveState"/>
/// (1=IsActive, 2=DeActive) rather than the previously-wrong <c>bool?</c> — but this query still
/// does not filter on it: including deactivated تفصیلی rows in this lookup remains a known,
/// deliberately-deferred gap (adding an active/inactive filter would be a new business rule, out
/// of scope here), matching the reference project's own behaviour (it does not filter
/// <c>ISACTIVE</c> in this lookup either).
/// </summary>
/// <param name="AccountCodeId">TB_ACCOUNTCODE.ID.</param>
/// <param name="LevelId">
/// TB_LEVEL_TAFSIL.ID — from <see cref="Accounting.Application.AccountCodes.Queries.GetTafsiliLevels.TafsiliLevelDto.LevelId"/>.
/// </param>
/// <param name="Search">Optional free-text filter — see class doc for the digit-vs-name heuristic.</param>
/// <param name="PageNumber">1-based page number.</param>
/// <param name="PageSize">Page size, capped by <see cref="GetTafsiliLevelItemsQueryValidator.MaxPageSize"/>.</param>
public sealed record GetTafsiliLevelItemsQuery(
    Guid AccountCodeId,
    Guid LevelId,
    string? Search,
    int PageNumber,
    int PageSize) : IRequest<PagedResult<TafsiliLookupItemDto>>, IVahedScopedQuery
{
    /// <summary>
    /// Organizational unit code driving Rule B. Server-assigned by <c>VahedScopeBehavior</c> from
    /// the authenticated caller's own <c>VahedCode</c> — never bound from client input. Mirrors
    /// the exact shape of every other <see cref="IVahedScopedQuery"/> implementer (e.g.
    /// <c>Accounting.Application.Expenses.Queries.GetExpenses.GetExpensesQuery.VahedCode</c>) —
    /// see that property's XML doc for the full [JsonIgnore]/settable-property rationale.
    /// </summary>
    [JsonIgnore]
    public string VahedCode { get; set; } = string.Empty;
}
