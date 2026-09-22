namespace Accounting.Domain.ValueObjects;

/// <summary>
/// Payment/receipt document type — maps the numeric value of column
/// <c>TB_PAYRECIVHEAD.PAYRECIVTYPE</c> (previously, incorrectly, <c>bool?</c>; see open risk #2 in
/// <c>CLAUDE.md</c> and <c>docs/centralaccount-business-reference.md</c> §24-1) to this enum.
///
/// Equivalent to enum <c>PayRecivType</c> in the reference project
/// (<c>D:\CentralAccount\Tamin.Core\Entities\PayAndReciv\PayRecivType.cs</c>): <c>pay = 1</c>
/// (توضیح <c>[Description("پرداخت")]</c>), <c>Recive = 2</c> (توضیح <c>[Description("دریافت")]</c>),
/// <c>All = 3</c> (توضیح <c>[Description("همه")]</c>) — confirmed by reading the reference file
/// directly, not guessed.
///
/// **Important:** the previous <c>bool?</c> mapping could only ever express two of these three
/// real values (plus NULL) — the third value (<see cref="All"/> = 3) was structurally unreachable
/// through this API before this fix (CLAUDE.md phase 12 finding, confirmed CONFIRMED in
/// §24-1 row 14).
/// </summary>
public enum PayRecivType
{
    /// <summary>پرداخت (payment) — equivalent to <c>pay = 1</c> in the reference project.</summary>
    Pay = 1,

    /// <summary>دریافت (receipt) — equivalent to <c>Recive = 2</c> in the reference project.</summary>
    Recive = 2,

    /// <summary>همه (both/all) — equivalent to <c>All = 3</c> in the reference project. Unreachable under the previous <c>bool?</c> mapping.</summary>
    All = 3,
}
