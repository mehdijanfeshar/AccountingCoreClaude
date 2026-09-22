namespace Accounting.Domain.ValueObjects;

/// <summary>
/// Financial-signatory role for a person-action grant window — maps the numeric value of column
/// <c>TB_PERSON_ACTION.OPERATORROLE</c> (previously, incorrectly, non-nullable <c>bool</c>; see
/// open risk #2 in <c>CLAUDE.md</c> and <c>docs/centralaccount-business-reference.md</c> §24-1) to
/// this enum.
///
/// Equivalent to enum <c>OperatorRole</c> in the reference project
/// (<c>D:\CentralAccount\Tamin.Core\Entities\PersonActions\OperatorRole.cs</c>): <c>MasolOmorMali
/// = 1</c> (توضیح <c>[Description("مسئول امور مالی ")]</c>), <c>ReyisVahed = 2</c> (توضیح
/// <c>[Description("رئیس واحد")]</c>), <c>JaneshinOmorMali = 3</c> (توضیح
/// <c>[Description("جانشین امور مالی")]</c>), <c>JaneshinReyisVahed = 4</c> (توضیح
/// <c>[Description("جانشین رئیس واحد")]</c>) — confirmed by reading the reference file directly,
/// not guessed.
///
/// Member names are kept identical to the reference project (transliterated Persian) rather than
/// translated into an English guess, since they are already unambiguous and clearer than any
/// English equivalent we could invent.
///
/// 🔴 Deliberately NOT addressed here (see CLAUDE.md open risk #1-ب): <c>TB_PERSON_ACTION</c> as a
/// whole remains completely unprotected (no ownership check on Create/Update/list). This batch
/// only fixes the CLR type of <c>OPERATORROLE</c>; it does not add authorization.
/// </summary>
public enum OperatorRole
{
    /// <summary>مسئول امور مالی — equivalent to <c>MasolOmorMali = 1</c> in the reference project.</summary>
    MasolOmorMali = 1,

    /// <summary>رئیس واحد — equivalent to <c>ReyisVahed = 2</c> in the reference project.</summary>
    ReyisVahed = 2,

    /// <summary>جانشین امور مالی — equivalent to <c>JaneshinOmorMali = 3</c> in the reference project.</summary>
    JaneshinOmorMali = 3,

    /// <summary>جانشین رئیس واحد — equivalent to <c>JaneshinReyisVahed = 4</c> in the reference project.</summary>
    JaneshinReyisVahed = 4,
}
