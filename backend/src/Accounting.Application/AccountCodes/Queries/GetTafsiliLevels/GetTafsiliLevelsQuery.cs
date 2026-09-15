using MediatR;

namespace Accounting.Application.AccountCodes.Queries.GetTafsiliLevels;

/// <summary>
/// Returns every "active" تفصیلی level configured for a معین (<c>TB_ACCOUNTCODE</c>), via the
/// domain chain <c>TB_ACCOUNTCODE → TB_ACCOUNT_LINK_LEVEL (+ TB_LEVEL_TAFSIL)</c>. Backs
/// <c>GET /api/account-codes/{accountCodeId}/tafsili-levels</c> (phase 20-b).
///
/// <b>Rule A — "active level" semantics</b> (CLAUDE.md open risk #12 /
/// <c>docs/centralaccount-business-reference.md</c> §3-3, resolved): the mere EXISTENCE of a
/// non-deleted <c>TB_ACCOUNT_LINK_LEVEL</c> row for (account, level) means that level is BOTH
/// allowed AND required. There is no <c>ISREQUIRED</c> column and no "allowed but optional"
/// state — see <see cref="TafsiliLevelDto.IsRequired"/>, which is always <see langword="true"/>
/// by construction to make that fact explicit in the contract rather than re-derived on the
/// frontend. Level numbers (<see cref="TafsiliLevelDto.Code"/>) are 1..7 and come from
/// <c>TB_LEVEL_TAFSIL.LEVEL_CODE</c>, a <see cref="string"/> column parsed defensively — see
/// <c>TafsiliLookupReadRepository.GetActiveLevelsAsync</c> for why an unparsable row is skipped
/// rather than crashing the request.
///
/// <b>Deliberately NOT <see cref="Accounting.Application.Common.Security.IVahedScopedQuery"/>.</b>
/// Neither <c>TB_ACCOUNT_LINK_LEVEL</c>, <c>TB_LEVEL_TAFSIL</c>, nor <c>TB_ACCOUNTCODE</c> has a
/// <c>VAHEDCODE</c> column — the chart of accounts is global in this schema (precedent:
/// <c>GetAccountCodesQuery</c> is likewise unscoped). There is no client-supplied
/// <c>vahedCode</c> anywhere in this endpoint, so the phase-19 invariant ("never trust a
/// caller-supplied unit code") holds trivially, and this endpoint never returns 403.
///
/// Not paged — a معین has at most 7 levels, so the whole set is returned as a bare JSON array.
/// Returns an empty array (200, not 404) for an unknown/levelless <see cref="AccountCodeId"/> —
/// an account with no configured levels is a legitimate, common state, indistinguishable from a
/// bad id at this layer.
/// </summary>
/// <param name="AccountCodeId">TB_ACCOUNTCODE.ID — the معین to look up levels for.</param>
public sealed record GetTafsiliLevelsQuery(Guid AccountCodeId) : IRequest<IReadOnlyList<TafsiliLevelDto>>;
