using Accounting.Domain.Entity;

namespace Accounting.Application.Common.Interfaces;

/// <summary>
/// Write-side repository for <see cref="TB_PREDESCRIB"/>. Only stages changes — it does NOT
/// call SaveChanges; the handler owns the transaction boundary via <see cref="IUnitOfWork"/>.
///
/// Deliberately exposes NO delete method: <c>TB_PREDESCRIB</c> has no <c>ISDELETED</c> column
/// (see <c>Accounting.Application.Tests.PreDescribs.PreDescribSchemaAssumptionsTests</c>, which
/// locks this in with reflection), and this project never issues physical deletes. There is
/// therefore no safe delete path for this entity — see
/// <c>Accounting.Api.Controllers.PreDescribsController</c> XML doc for the full rationale.
/// </summary>
public interface IPreDescribRepository
{
    Task AddAsync(TB_PREDESCRIB preDescrib, CancellationToken cancellationToken = default);

    /// <summary>
    /// Loads a single <see cref="TB_PREDESCRIB"/> by <c>ID</c> as a change-tracked entity
    /// (deliberately no <c>AsNoTracking()</c>, unlike the read repository) so that the Update
    /// handler can mutate the returned instance in place and have EF Core generate the correct
    /// UPDATE on <see cref="IUnitOfWork.SaveChangesAsync"/>. Returns <see langword="null"/>
    /// when no row with that <c>ID</c> exists.
    /// </summary>
    Task<TB_PREDESCRIB?> GetForUpdateAsync(Guid id, CancellationToken cancellationToken = default);
}
