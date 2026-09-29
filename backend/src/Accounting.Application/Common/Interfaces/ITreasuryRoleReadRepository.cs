using Accounting.Application.Treasury.Queries;

namespace Accounting.Application.Common.Interfaces;

/// <summary>
/// Read-side repository for <see cref="Accounting.Domain.Entity.TB_TR_ROLE"/> — backs
/// <c>GET api/treasury/roles</c>. Never stages changes, always returns DTO projections.
/// </summary>
public interface ITreasuryRoleReadRepository
{
    /// <summary>Every active (<c>ISDELETED == false</c>) role row for <paramref name="vahedCode"/>.</summary>
    Task<IReadOnlyList<TreasuryRoleDto>> GetByVahedAsync(string vahedCode, CancellationToken cancellationToken = default);
}
