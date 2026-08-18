namespace Accounting.Application.Common;

/// <summary>
/// Simple offset-based paged result shared by every list Query in the read model. Deliberately
/// minimal (no cursor/keyset paging) — <see cref="PageNumber"/> is 1-based, <see cref="TotalCount"/>
/// is computed by the repository via a <c>CountAsync</c> against the same filter used for
/// <see cref="Items"/>.
/// </summary>
/// <typeparam name="T">The projected DTO type of a single page item.</typeparam>
public sealed class PagedResult<T>
{
    public IReadOnlyList<T> Items { get; init; } = Array.Empty<T>();

    public int PageNumber { get; init; }

    public int PageSize { get; init; }

    public int TotalCount { get; init; }
}
