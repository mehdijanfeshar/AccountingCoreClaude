namespace Accounting.Domain.ValueObjects;

/// <summary>
/// Identification-digit attribute value kind (numeric/date) — maps the numeric value of column
/// <c>TB_ATTRIBFORACCOUNTCODE.FLAG</c> (previously, incorrectly, non-nullable <c>bool</c>; see
/// open risk #2 in <c>CLAUDE.md</c> and <c>docs/centralaccount-business-reference.md</c> §24-1) to
/// this enum.
///
/// Equivalent to enum <c>FlagEnum</c> in the reference project
/// (<c>D:\CentralAccount\Tamin.Core\Entities\Attribs\FlagEnum.cs</c>): <c>Number = 1</c> (توضیح
/// <c>[Description("عدد")]</c>), <c>Date = 2</c> (توضیح <c>[Description("تاریخ")]</c>) — confirmed
/// by reading the reference file directly, not guessed.
///
/// ⚠️ <b>Deliberately renamed from the reference project's bare <c>FlagEnum</c> to
/// <c>AttribFlag</c>.</b> A name as generic as <c>FlagEnum</c> would be actively misleading in our
/// shared <c>ValueObjects</c> namespace, which already hosts many unrelated types — a future
/// reader searching for "the FLAG column enum" would have no way to tell which table it belongs
/// to from the name alone. Cross-reference this XML doc (reference file path + original enum
/// name) if you need to diff against the source project.
/// </summary>
public enum AttribFlag
{
    /// <summary>عدد (numeric) — equivalent to <c>Number = 1</c> (<c>FlagEnum.Number</c>) in the reference project.</summary>
    Number = 1,

    /// <summary>تاریخ (date) — equivalent to <c>Date = 2</c> (<c>FlagEnum.Date</c>) in the reference project.</summary>
    Date = 2,
}
