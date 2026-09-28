using Accounting.Domain.ValueObjects;

namespace Accounting.Application.Common.Security;

/// <summary>
/// The single place <see cref="PettyCashDocState"/> (seven values, petty-cash-specific) is
/// translated into the Legacy <see cref="ChargeAndCostStatus"/> (three values, shared with every
/// other <c>TB_CHARGEANDCOST_HEAD</c> consumer) that every write path stamps onto the head row's
/// <c>STATUS</c> column. No handler performs this translation inline — see
/// <c>docs/tankhah-khazaneh-module.md</c> §۲: "نگاشت به STATUS Legacy فقط در یک جا انجام
/// می‌شود".
///
/// <b>The rule</b> (design doc §2): only <see cref="PettyCashDocState.Approved"/> (تأییدشده،
/// منتظر ترمیم) and <see cref="PettyCashDocState.Settled"/> (تسویه‌شده) map to
/// <see cref="ChargeAndCostStatus.Accepted"/>. Every other state — including
/// <see cref="PettyCashDocState.Rejected"/>, which is terminal but never "accepted" — maps to
/// <see cref="ChargeAndCostStatus.Temporary"/>. <see cref="ChargeAndCostStatus.Reviewed"/> is
/// never produced by this map: the reference project's <c>GetAllAccepted</c> query (the source of
/// the "تأییدشده → ۲" rule) only ever branches on temporary vs. accepted for this table.
/// </summary>
public static class PettyCashStatusMap
{
    public static ChargeAndCostStatus ToLegacyStatus(PettyCashDocState state) =>
        state is PettyCashDocState.Approved or PettyCashDocState.Settled
            ? ChargeAndCostStatus.Accepted
            : ChargeAndCostStatus.Temporary;

    /// <summary>
    /// بخش ۳-الف (۲۰۲۶-۰۹-۲۸، <c>docs/tankhah-khazaneh-module.md</c>) — same "one mapping, one
    /// home" rule, extended to <see cref="PettyCashReplenishmentState"/> (the ترمیم/شارژ side of
    /// <c>TB_CHARGEANDCOST_HEAD</c>). Only <see cref="PettyCashReplenishmentState.Paid"/> maps to
    /// <see cref="ChargeAndCostStatus.Accepted"/> — mirrors <see cref="ToLegacyStatus(PettyCashDocState)"/>'s
    /// "only the state that means money has actually, finally moved counts as accepted" rule.
    /// </summary>
    public static ChargeAndCostStatus ToLegacyStatus(PettyCashReplenishmentState state) =>
        state == PettyCashReplenishmentState.Paid
            ? ChargeAndCostStatus.Accepted
            : ChargeAndCostStatus.Temporary;
}
