using Accounting.Application.AccountCodes.Queries.GetTafsiliLevelItems;
using Accounting.Application.Common;

namespace Accounting.Application.Common.Interfaces;

/// <summary>
/// Read-side repository backing خزانه‌داری's «تفصیلی ذی‌نفع» picker — اصلاح ۴-الف (۲۰۲۶-۰۹-۲۹،
/// صاحب پروژه؛ <c>docs/tankhah-khazaneh-module.md</c> §۱۰). Both members read the same domain
/// chain: <c>TB_TAFSIL_GROUP → TB_TAFSIL_LINK_TAFSILGROUP → TB_TAFSILI</c>, scoped to ONE group id
/// (the unit's configured <c>TB_TR_SETTING.BENEFICIARY_TAFSIL_GROUP_ID</c> — resolved by the
/// caller, not by this repository, unlike <c>ITafsiliLookupReadRepository</c>'s Rule B which needs
/// the caller's own unit). No write side — this is a read-only lookup, same as
/// <c>ITafsiliLookupReadRepository</c>.
/// </summary>
public interface ITreasuryBeneficiaryTafsiliReadRepository
{
    /// <summary>
    /// True when an active (<c>ISDELETED == false</c>) <c>TB_TAFSIL_LINK_TAFSILGROUP</c> row links
    /// <paramref name="tafsiliId"/> to <paramref name="tafsilGroupId"/>. Backs the 400 membership
    /// check on Create/Update — deliberately does not also assert <c>TB_TAFSILI</c> itself is
    /// non-deleted (the link row's own ISDELETED is the rule's actual scope; a stale link to an
    /// otherwise-deleted تفصیلی is an existing data-quality question, not this rule's job).
    /// </summary>
    Task<bool> IsMemberOfGroupAsync(
        Guid tafsiliId, Guid tafsilGroupId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns a page of تفصیلی belonging to <paramref name="tafsilGroupId"/> — backs
    /// <c>GET api/treasury/beneficiary-tafsilis</c>. Optionally filtered by
    /// <paramref name="search"/>, using the same digit-vs-name heuristic as
    /// <c>ITafsiliLookupReadRepository.GetSelectableItemsAsync</c> (a term containing any digit
    /// matches <c>TAFSILI_CODE</c> only; otherwise <c>TAFSILI_NAME</c> only).
    /// </summary>
    Task<PagedResult<TafsiliLookupItemDto>> GetPagedAsync(
        Guid tafsilGroupId,
        string? search,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default);
}
