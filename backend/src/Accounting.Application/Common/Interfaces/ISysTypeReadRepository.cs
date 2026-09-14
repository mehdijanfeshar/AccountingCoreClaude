using Accounting.Application.SysTypes.Queries;

namespace Accounting.Application.Common.Interfaces;

/// <summary>
/// Read-side repository for <c>TB_SYSTYPE</c> (نوع سند). Read-only by design — this project
/// never writes to this reference table, so there is no matching write-side repository.
/// </summary>
public interface ISysTypeReadRepository
{
    /// <summary>
    /// Returns every row, ordered by <c>SYS_COD</c>. Not paged: the table holds a handful of
    /// fixed rows and every caller wants the whole list to populate a picker.
    /// </summary>
    Task<IReadOnlyList<SysTypeDto>> GetAllAsync(CancellationToken cancellationToken = default);
}
