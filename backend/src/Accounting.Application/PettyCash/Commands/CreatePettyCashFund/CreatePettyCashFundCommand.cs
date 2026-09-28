using System.Text.Json.Serialization;
using Accounting.Application.Common.Security;
using Accounting.Domain.ValueObjects;
using MediatR;

namespace Accounting.Application.PettyCash.Commands.CreatePettyCashFund;

/// <summary>
/// <c>POST api/petty-cash/funds</c> — creates a new <c>TB_PC_FUND</c> row. This is the module's
/// own, fully independent تنخواه definition (2026-09-28 decision,
/// <c>docs/tankhah-khazaneh-module.md</c> §0) — it does not touch <c>TB_REVOLVING_FUND</c> at all.
/// Carries primitive fields only — the handler is responsible for constructing the Domain entity.
/// Returns the newly generated <see cref="Guid"/> ID.
///
/// <c>Code</c> is protected against duplicates within the caller's unit by an explicit
/// application-level check (<see cref="Accounting.Application.Common.Exceptions.PettyCashFundCodeDuplicateException"/>,
/// 409) as well as the real DB constraint <c>UK_PC_FUND_CODE</c>. <c>AccountCodeId</c>, when
/// supplied, must reference an existing, non-deleted <c>TB_ACCOUNTCODE</c> row (404 otherwise) —
/// verified explicitly in the handler rather than left to the FK-violation-to-400 fallback, since
/// this batch's task description calls for it.
/// </summary>
/// <param name="Code">CODE column (required; part of <c>UK_PC_FUND_CODE</c>).</param>
/// <param name="Name">NAME column (required).</param>
/// <param name="CustodianUserId">CUSTODIAN_USERID column (required) — تنخواه‌دار مسئول, in the
/// <c>ICurrentUser.UserId</c>/<c>ADDUSERID</c> identity space, not a national id.</param>
/// <param name="CustodianName">CUSTODIAN_NAME column (optional display name).</param>
/// <param name="Ceiling">CEILING column (required, &gt; 0) — سقف تنخواه.</param>
/// <param name="PerDocLimit">PER_DOC_LIMIT column (required, &gt; 0 and ≤ <see cref="Ceiling"/>) — سقف هر سند.</param>
/// <param name="FinanceManagerApprovalLimit">FINANCE_MANAGER_APPROVAL_LIMIT column (required, &gt; 0)
/// — تکمیل بخش ۲ (۲۰۲۶-۰۹-۲۸، صفحهٔ ۱۳ پاورپوینت): سقف اختیار تأیید نهایی نقش مدیر مالی؛ بیشتر از
/// این فقط مدیرعامل می‌تواند تأیید نهایی کند.</param>
/// <param name="AlertThresholdPercent">ALERT_THRESHOLD_PERCENT column (optional, 0..100).</param>
/// <param name="AccountCodeId">Optional link to <c>TB_ACCOUNTCODE</c> (<c>FK_PC_FUND_ACCOUNTCODE</c>).</param>
/// <param name="SettlementPeriod">SETTLEMENT_PERIOD column (optional).</param>
/// <param name="IsActive">IS_ACTIVE column — an inactive fund accepts no new صورت‌هزینه/Submit.</param>
public sealed record CreatePettyCashFundCommand(
    string Code,
    string Name,
    string CustodianUserId,
    string? CustodianName,
    decimal Ceiling,
    decimal PerDocLimit,
    decimal FinanceManagerApprovalLimit,
    int? AlertThresholdPercent,
    Guid? AccountCodeId,
    PettyCashSettlementPeriod? SettlementPeriod,
    bool IsActive) : IRequest<Guid>, IVahedScopedCommand
{
    /// <summary>
    /// Organizational unit code — never bound from the request body
    /// (<see cref="JsonIgnoreAttribute"/>) and never trusted even if a caller manages to set it:
    /// <c>VahedScopeBehavior</c> unconditionally overwrites this with the authenticated caller's
    /// own unit code before the request reaches <c>CreatePettyCashFundCommandHandler</c>.
    /// </summary>
    [JsonIgnore]
    public string VahedCode { get; set; } = string.Empty;
}
