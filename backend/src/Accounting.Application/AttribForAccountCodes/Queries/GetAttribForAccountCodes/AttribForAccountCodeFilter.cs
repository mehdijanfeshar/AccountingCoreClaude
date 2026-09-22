using Accounting.Domain.ValueObjects;

namespace Accounting.Application.AttribForAccountCodes.Queries.GetAttribForAccountCodes;

/// <summary>
/// Server-side filters for the "حساب‌های شناسه‌دار" list, ported from the reference Angular app's
/// <c>base-identity-account</c> search panel (`MoinCode` &gt;= / &lt;=, `AttribSum`, `flag`,
/// `Year`). Grouped into one record — following the <c>VoucherHeadFilter</c> precedent — so the
/// repository signature does not grow one positional parameter per filter.
///
/// The organizational unit is deliberately NOT a filter here, exactly as on the voucher list:
/// every query stays scoped to the caller's own unit via <c>VahedScopeBehavior</c> (open risk #1).
/// The reference app did send <c>VahedCode</c> as a client-supplied search param; we do not, and
/// must not.
///
/// ⚠️ <b>Note the deliberate difference from <c>VoucherHeadFilter.DocNumFrom/To</c>.</b> That one
/// has to compare string length before comparing lexicographically, because <c>DOC_NUM</c> holds
/// numbers of mixed width (live data has both <c>"054354"</c> and <c>"20"</c>). <c>ACCCODE</c> has
/// no such problem: a معین code is <b>always exactly 6 digits</b> — enforced by the reference
/// project's own validator and confirmed as the project owner's 2/4/6-digit structure (see
/// <c>docs/centralaccount-business-reference.md</c> §۲-۱). For fixed-width numeric strings a plain
/// lexicographic range is already correct, so do not cargo-cult the voucher trick here.
/// </summary>
/// <param name="MoinCodeFrom">Inclusive lower bound on <c>TB_ACCOUNTCODE.ACCCODE</c> of the linked account.</param>
/// <param name="MoinCodeTo">Inclusive upper bound on <c>TB_ACCOUNTCODE.ACCCODE</c> of the linked account.</param>
/// <param name="AttribSum">Exact match on <c>ATTRIBSUM</c> (جمع‌پذیری).</param>
/// <param name="Flag">Exact match on <c>FLAG</c> (نوع مقدار).</param>
/// <param name="Year">Exact match on <c>YEAR</c> (سال مالی).</param>
public sealed record AttribForAccountCodeFilter(
    string? MoinCodeFrom = null,
    string? MoinCodeTo = null,
    AttribSum? AttribSum = null,
    AttribFlag? Flag = null,
    string? Year = null);
