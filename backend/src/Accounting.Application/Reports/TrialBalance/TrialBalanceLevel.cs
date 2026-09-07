namespace Accounting.Application.Reports.TrialBalance;

/// <summary>
/// Which row of the coding hierarchy a trial balance report aggregates by. Values deliberately
/// mirror <c>TB_ACCOUNTCODE.TYPECODE</c> (Group = 1, Kol = 2, Moin = 3 — see the
/// <c>bool?</c>→enum bug noted in <c>CLAUDE.md</c> "فاز ۱۲"; this enum is the correct C# shape,
/// the physical column is just read/written as a raw number by the trial balance query to work
/// around that bug, not fixed here).
///
/// Level 4 (تفصیلی) is intentionally NOT a member of this enum — تفصیلی is not itself a row of
/// <c>TB_ACCOUNTCODE</c> (it lives in <c>TB_TAFSILI</c>/<c>TB_VOUCHERDETAIL_LINK_TAFSILI</c>) and
/// aggregating by it requires a structurally different join, out of scope for this batch. A later
/// batch adds it once that join shape is decided.
/// </summary>
public enum TrialBalanceLevel
{
    /// <summary>گروه — mirrors <c>TB_ACCOUNTCODE.TYPECODE = 1</c>.</summary>
    Group = 1,

    /// <summary>کل — mirrors <c>TB_ACCOUNTCODE.TYPECODE = 2</c>.</summary>
    Kol = 2,

    /// <summary>معین — mirrors <c>TB_ACCOUNTCODE.TYPECODE = 3</c>. The only level the underlying
    /// query currently supports at the leaf (<c>a</c>) join, since the report is always scoped to
    /// معین-level voucher detail lines (<c>WHERE a.TYPECODE = 3</c>) and rolls that up to Kol/Group
    /// via the <c>PARENTID</c> self-join chain.</summary>
    Moin = 3,
}
