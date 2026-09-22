using Accounting.Application.Years.Queries;

namespace Accounting.Application.Common.Interfaces;

/// <summary>
/// Read-side repository for <c>TB_YEAR</c>. There is no write-side counterpart: this project
/// creates no financial years (the reference project's <c>TbYear/CreateFinancialYear</c> has no
/// equivalent here and is out of scope for phase 37).
///
/// No <c>ISDELETED</c> filter is applied or possible — <c>TB_YEAR</c> has no such column, and no
/// audit columns either.
/// </summary>
public interface IYearReadRepository
{
    /// <summary>
    /// Returns every financial year, newest first.
    /// </summary>
    Task<IReadOnlyList<YearDto>> GetAllAsync(CancellationToken cancellationToken = default);
}
