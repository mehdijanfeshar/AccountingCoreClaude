namespace Accounting.Domain.ValueObjects;

/// <summary>
/// Identification-digit control kind (non-zero/date) — maps the numeric value of column
/// <c>TB_ATTRIBFORACCOUNTCODE.CONTROLID</c> (previously, incorrectly, <c>bool?</c>; see open risk
/// #2 in <c>CLAUDE.md</c> and <c>docs/centralaccount-business-reference.md</c> §24-1) to this
/// enum.
///
/// Equivalent to enum <c>ControlEnum</c> in the reference project
/// (<c>D:\CentralAccount\Tamin.Core\Entities\Attribs\ControlEnum.cs</c>): <c>NotZero = 1</c>
/// (توضیح <c>[Description("غیرصفر")]</c>), <c>IsDate = 2</c> (توضیح <c>[Description("تاریخ")]</c>)
/// — confirmed by reading the reference file directly, not guessed.
///
/// ⚠️ <b>Deliberately renamed from the reference project's bare <c>ControlEnum</c> to
/// <c>AttribControl</c>.</b> A name as generic as <c>ControlEnum</c> would collide semantically
/// with many unrelated "control" concepts in our shared <c>ValueObjects</c> namespace. Cross-
/// reference this XML doc (reference file path + original enum name) if you need to diff against
/// the source project.
///
/// ⚠️ <b>Odd pre-existing mapping oddity, NOT fixed here:</b> the Fluent mapping on this column
/// combines <c>.IsRequired()</c> with <c>.HasDefaultValueSql("null ")</c> while the CLR property
/// stays nullable (<c>AttribControl?</c>) — a contradiction already flagged in
/// <c>docs/centralaccount-business-reference.md</c> (row on <c>TB_ATTRIBFORACCOUNTCODE.CONTROLID</c>,
/// "DEFAULT با NOT NULL متناقض"). This batch only fixes the CLR type, not that oddity.
/// </summary>
public enum AttribControl
{
    /// <summary>غیرصفر (non-zero) — equivalent to <c>NotZero = 1</c> (<c>ControlEnum.NotZero</c>) in the reference project.</summary>
    NotZero = 1,

    /// <summary>تاریخ (date) — equivalent to <c>IsDate = 2</c> (<c>ControlEnum.IsDate</c>) in the reference project.</summary>
    IsDate = 2,
}
