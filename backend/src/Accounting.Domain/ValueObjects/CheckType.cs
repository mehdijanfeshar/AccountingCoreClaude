namespace Accounting.Domain.ValueObjects;

/// <summary>
/// Cheque type (real/token) — maps the numeric value of column
/// <c>TB_CHECKBOOK.CHECKBOOK_TYPE</c> (previously, incorrectly, <c>bool?</c>; see open risk #2 in
/// <c>CLAUDE.md</c> and <c>docs/centralaccount-business-reference.md</c> §24-1) to this enum.
///
/// Equivalent to enum <c>CheckType</c> in the reference project
/// (<c>D:\CentralAccount\Tamin.Core\Entities\Checks\CheckType.cs</c>): <c>sori = 1</c> (توضیح
/// <c>[Description("چک صوری")]</c>), <c>real = 2</c> (توضیح <c>[Description("چک واقعی")]</c>) —
/// confirmed by reading the reference file directly, not guessed.
///
/// ⚠️ <b>§24-3 caveat, carried forward, not silently dropped:</b> in the reference project this
/// column is a <b>hardcoded server-side constant</b> (<c>CheckType.real</c>) on both Create and
/// Update — it is never a caller input at all (the only place <c>sori</c> is ever written is the
/// unrelated <c>AddUnitFinancialYear</c> system path). This phase (27 batch 2) only fixes the CLR
/// <b>type</b> of the column; it deliberately does NOT change whether the field stays
/// caller-supplied on our API — that "should this even be an input?" question remains open
/// (CLAUDE.md open risk #2-ب).
/// </summary>
public enum CheckType
{
    /// <summary>چک صوری (token/placeholder cheque) — equivalent to <c>sori = 1</c> in the reference project.</summary>
    Sori = 1,

    /// <summary>چک واقعی (real cheque) — equivalent to <c>real = 2</c> in the reference project.</summary>
    Real = 2,
}
