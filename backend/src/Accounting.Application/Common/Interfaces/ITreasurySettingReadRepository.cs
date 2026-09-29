using Accounting.Application.Treasury.Queries;

namespace Accounting.Application.Common.Interfaces;

/// <summary>
/// Read-side repository for <see cref="Accounting.Domain.Entity.TB_TR_SETTING"/> — backs
/// <c>GET api/treasury/settings</c> and every Submit/Approve rule that needs the unit's
/// <c>CEO_APPROVAL_THRESHOLD</c>/<c>BULK_APPROVE_LIMIT</c>. Never stages changes.
/// </summary>
public interface ITreasurySettingReadRepository
{
    /// <summary>Returns the settings row for <paramref name="vahedCode"/>, or <see langword="null"/>
    /// when the unit's مدیر مالی has not defined one yet.</summary>
    Task<TreasurySettingDto?> GetByVahedAsync(string vahedCode, CancellationToken cancellationToken = default);
}
