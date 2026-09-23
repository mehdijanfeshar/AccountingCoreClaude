using Accounting.Domain.ValueObjects;

namespace Accounting.Application.WhiteAndBlackLists.Queries;

/// <summary>
/// The optional filter set understood by
/// <see cref="Common.Interfaces.IWhiteAndBlackListReadRepository.GetPagedAsync"/>. Every member
/// is nullable and means "do not filter on this" when null; the non-null ones combine with AND.
///
/// This exists as its own type (rather than seven parameters on the repository method) so that a
/// new filter can be added without changing the repository signature — the same reason
/// <c>SearchParam</c> exists on the reference project's screens. It is a plain value carrier: it
/// holds no behaviour and performs no validation, which stays in
/// <see cref="GetWhiteAndBlackLists.GetWhiteAndBlackListsQueryValidator"/>.
/// </summary>
/// <param name="AccountCodeId">Equality filter on <c>ACCOUNTCODE_ID</c>.</param>
/// <param name="VahedTypeId">Equality filter on <c>VAHEDTYPE_ID</c>.</param>
/// <param name="State">Equality filter on <c>STATE</c>.</param>
/// <param name="FromAuthorizedDate">Inclusive lower bound on <c>FROMAUTHORIZEDDATE</c> (string compare; see the query XML doc).</param>
/// <param name="ToAuthorizedDate">Inclusive upper bound on <c>TOAUTHORIZEDDATE</c>.</param>
/// <param name="FromLimitationDate">Inclusive lower bound on <c>FROMLIMITATIONDATE</c>.</param>
/// <param name="ToLimitationDate">Inclusive upper bound on <c>TOLIMITATIONDATE</c>.</param>
public sealed record WhiteAndBlackListFilter(
    Guid? AccountCodeId = null,
    Guid? VahedTypeId = null,
    WhiteBlackListState? State = null,
    string? FromAuthorizedDate = null,
    string? ToAuthorizedDate = null,
    string? FromLimitationDate = null,
    string? ToLimitationDate = null)
{
    /// <summary>A filter that excludes nothing. Use instead of constructing an all-null instance inline.</summary>
    public static readonly WhiteAndBlackListFilter None = new();
}
