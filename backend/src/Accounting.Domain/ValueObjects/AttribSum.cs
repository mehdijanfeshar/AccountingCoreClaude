namespace Accounting.Domain.ValueObjects;

/// <summary>
/// Identification-digit summability — maps the numeric value of column
/// <c>TB_ATTRIBFORACCOUNTCODE.ATTRIBSUM</c> (previously, incorrectly, non-nullable <c>bool</c>;
/// see open risk #2 in <c>CLAUDE.md</c> and <c>docs/centralaccount-business-reference.md</c>
/// §24-1) to this enum.
///
/// Equivalent to enum <c>AttribSumEnum</c> in the reference project
/// (<c>D:\CentralAccount\Tamin.Core\Entities\Attribs\AttribSumEnum.cs</c>): <c>Summable = 1</c>
/// (توضیح <c>[Description("جمع پذیر")]</c>), <c>UnSummable = 2</c> (توضیح
/// <c>[Description("جمع ناپذیر")]</c>) — confirmed by reading the reference file directly, not
/// guessed.
///
/// ⚠️ <b>Deliberately renamed from the reference project's bare <c>AttribSumEnum</c> to
/// <c>AttribSum</c>.</b> Kept for symmetry with the sibling renames on this same table
/// (<see cref="AttribFlag"/>, <see cref="AttribControl"/>) — the <c>Enum</c> suffix is redundant
/// noise once the type already lives among other enums in <c>ValueObjects</c>. Cross-reference
/// this XML doc (reference file path + original enum name) if you need to diff against the source
/// project.
/// </summary>
public enum AttribSum
{
    /// <summary>جمع‌پذیر (summable) — equivalent to <c>Summable = 1</c> (<c>AttribSumEnum.Summable</c>) in the reference project.</summary>
    Summable = 1,

    /// <summary>جمع‌ناپذیر (not summable) — equivalent to <c>UnSummable = 2</c> (<c>AttribSumEnum.UnSummable</c>) in the reference project.</summary>
    UnSummable = 2,
}
