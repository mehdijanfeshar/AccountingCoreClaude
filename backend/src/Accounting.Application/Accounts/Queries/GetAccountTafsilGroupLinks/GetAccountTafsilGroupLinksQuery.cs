using MediatR;

namespace Accounting.Application.Accounts.Queries.GetAccountTafsilGroupLinks;

/// <summary>
/// Returns every non-deleted "ارتباط معین با گروه تفصیلی" link (<c>TB_ACCOUNT_LINK_TAFSILGROUP</c>)
/// configured for a معین. Backs <c>GET /api/account-codes/{accountCodeId}/tafsil-group-links</c>.
///
/// Deliberately NOT <see cref="Accounting.Application.Common.Security.IVahedScopedQuery"/> —
/// <c>TB_ACCOUNT_LINK_TAFSILGROUP</c> has no <c>VAHEDCODE</c> column, mirroring
/// <c>GetTafsiliLevelsQuery</c>'s reasoning for the sibling <c>TB_ACCOUNT_LINK_LEVEL</c> table.
///
/// Not paged — a معین is expected to have very few such links; the whole set is returned as a
/// bare JSON array. Returns an empty array (200, not 404) for a معین with no configured links or
/// an unknown <see cref="AccountCodeId"/> — indistinguishable at this layer, same precedent as
/// <c>GetTafsiliLevelsQuery</c>.
/// </summary>
/// <param name="AccountCodeId">TB_ACCOUNTCODE.ID — the معین to look up links for.</param>
public sealed record GetAccountTafsilGroupLinksQuery(Guid AccountCodeId)
    : IRequest<IReadOnlyList<AccountTafsilGroupLinkDto>>;
