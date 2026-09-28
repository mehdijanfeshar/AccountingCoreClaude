using Accounting.Application.PettyCash.Queries;

namespace Accounting.Application.Common.Interfaces;

/// <summary>
/// Read-side repository for <c>TB_PC_REVIEWER</c> — backs
/// <c>GET api/petty-cash/funds/{fundId}/reviewers</c>. <c>AsNoTracking()</c>, like every other
/// petty-cash read repository.
/// </summary>
public interface IPettyCashFundReviewerReadRepository
{
    /// <summary>
    /// Every active (<c>ISDELETED == false</c>) reviewer row for <paramref name="fundId"/>, scoped
    /// to <paramref name="vahedCode"/>. Returns an empty list — never <see langword="null"/> — when
    /// there are none.
    /// </summary>
    Task<IReadOnlyList<PettyCashFundReviewerDto>> GetByFundIdAsync(
        Guid fundId,
        string vahedCode,
        CancellationToken cancellationToken = default);
}
