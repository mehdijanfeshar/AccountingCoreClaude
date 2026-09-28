using System.Text.Json.Serialization;
using Accounting.Application.Common.Security;
using Accounting.Domain.ValueObjects;
using MediatR;

namespace Accounting.Application.PettyCash.Commands.CreatePettyCashReplenishment;

/// <summary>
/// <c>POST api/petty-cash/replenishments</c> — بخش ۳-الف (<c>docs/tankhah-khazaneh-module.md</c>،
/// صفحهٔ ۹ پاورپوینت). Builds the composite ترمیم aggregate: one <c>TB_CHARGEANDCOST_HEAD</c>
/// (type <see cref="ChargeAndCostType.Charge"/>, <c>ACCOUNT_ID</c> = <see cref="SourceBankAccountId"/>),
/// one <c>TB_PC_REPLENISHMENT</c>, and one <c>TB_CHARGE_LINK_COST</c> row per unlinked تأییدشده
/// صورت‌هزینه of the fund (exactly the set <c>GetPettyCashReplenishmentPreviewQuery</c> would
/// return right now) — all persisted with a single <see cref="Accounting.Application.Common.Interfaces.IUnitOfWork.SaveChangesAsync"/>
/// call, so a link race with a concurrent ترمیم request is caught by
/// <c>IChargeAndCostRepository.ExistsActiveLinkForCostAsync</c> before any row is staged.
///
/// <c>RegisterDate</c>/<c>Year</c> exist for the same reason <c>CreatePettyCashExpenseDocCommand</c>
/// carries them — <c>TB_CHARGEANDCOST_HEAD.CHARGEANDCOST_DATE</c>/<c>YEAR</c> are Legacy
/// <c>NOT NULL</c> columns with no equivalent field named in the design doc's endpoint shape; the
/// same conservative، owner-precedented choice from بخش ۱ applies (caller/session supplies them,
/// server never derives a fiscal year from a free-text date).
/// </summary>
/// <param name="FundId">TB_PC_REPLENISHMENT.FUND_ID.</param>
/// <param name="SourceBankAccountId">حساب بانکی مبدأ — becomes <c>TB_CHARGEANDCOST_HEAD.ACCOUNT_ID</c>.</param>
/// <param name="PaymentMethod">TB_PC_REPLENISHMENT.PAYMENT_METHOD.</param>
/// <param name="RegisterDate">TB_CHARGEANDCOST_HEAD.CHARGEANDCOST_DATE (شمسی YYYYMMDD).</param>
/// <param name="Year">Fiscal year (session-selected, same as every other write in this module).</param>
/// <param name="Note">TB_PC_REPLENISHMENT.NOTE / TB_CHARGEANDCOST_HEAD.DESCRIPTION.</param>
/// <param name="Submit">When <see langword="true"/>, the ترمیم is created directly in
/// <see cref="PettyCashReplenishmentState.PendingFinanceManager"/> instead of
/// <see cref="PettyCashReplenishmentState.Draft"/> — same shape as
/// <c>CreatePettyCashExpenseDocCommand.Submit</c>.</param>
public sealed record CreatePettyCashReplenishmentCommand(
    Guid FundId,
    Guid SourceBankAccountId,
    PettyCashPaymentMethod PaymentMethod,
    string RegisterDate,
    string Year,
    string? Note,
    bool Submit) : IRequest<Guid>, IVahedScopedCommand
{
    [JsonIgnore]
    public string VahedCode { get; set; } = string.Empty;
}
