using Accounting.Domain.ValueObjects;

namespace Accounting.Application.PettyCash.Queries;

/// <summary>
/// Read-side projection of <c>TB_PC_FUND_SETTING</c> ("تنظیمات تنخواه"). Used both embedded in
/// <see cref="PettyCashFundDto.Settings"/> and as the standalone
/// <c>GET api/petty-cash/funds/{fundId}/settings</c> response — same shape both places, per the
/// frontend contract (<c>docs/tankhah-khazaneh-module.md</c> §5).
/// </summary>
/// <param name="CustodianUserId">CUSTODIAN_USERID column — تنخواه‌دار.</param>
/// <param name="CustodianName">CUSTODIAN_NAME column.</param>
/// <param name="PerDocLimit">PER_DOC_LIMIT column — سقف هر سند.</param>
/// <param name="AlertThresholdPercent">ALERT_THRESHOLD_PERCENT column.</param>
/// <param name="SettlementPeriod">SETTLEMENT_PERIOD column.</param>
public sealed record PettyCashFundSettingDto(
    string? CustodianUserId,
    string? CustodianName,
    decimal? PerDocLimit,
    int? AlertThresholdPercent,
    PettyCashSettlementPeriod? SettlementPeriod);
