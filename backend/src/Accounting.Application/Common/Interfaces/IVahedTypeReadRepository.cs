using Accounting.Application.VahedTypes.Queries;

namespace Accounting.Application.Common.Interfaces;

/// <summary>
/// Read-side repository for <c>TB_VAHED_TYPE</c>. There is no write-side counterpart on purpose:
/// this project never creates or edits organisational unit types — they arrive with the shared
/// <c>CENTRALACCOUNT</c> schema, exactly like <c>TB_SYSTYPE</c>. Adding a write path here would
/// let this application reshape a lookup other systems on the same schema depend on.
/// </summary>
public interface IVahedTypeReadRepository
{
    /// <summary>
    /// Returns every unit type ordered by <c>PARENTTYPECODE</c> then <c>TYPECODE</c>, so a
    /// caller can build the «بخش» → «نوع واحد» tree by a single pass without re-sorting.
    ///
    /// The table has no <c>ISDELETED</c> column, so there is nothing to filter — every row is
    /// live. That absence is asserted by <c>VahedTypeSchemaAssumptionsTests</c> so a future
    /// scaffold that adds one cannot silently start leaking deleted rows here.
    /// </summary>
    Task<IReadOnlyList<VahedTypeDto>> GetAllAsync(CancellationToken cancellationToken = default);
}
