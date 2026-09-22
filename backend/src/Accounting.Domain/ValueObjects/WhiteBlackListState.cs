namespace Accounting.Domain.ValueObjects;

/// <summary>
/// Allow/deny-list authorization state for an account code — maps the numeric value of column
/// <c>TB_WHITEANDBLACKLIST.STATE</c> (previously, incorrectly, <c>bool?</c>; see open risk #2 in
/// <c>CLAUDE.md</c> and <c>docs/centralaccount-business-reference.md</c> §24-1) to this enum.
///
/// Equivalent to enum <c>StateEnum</c> in the reference project
/// (<c>D:\CentralAccount\Tamin.Core\Entities\WhiteAndBlackLists\StateEnum.cs</c>): <c>Allowed =
/// 1</c> (توضیح <c>[Description("مجاز به ثبت دستی و سیستمی")]</c>), <c>SystemOnly = 2</c> (توضیح
/// <c>[Description(" فقط مجاز به ثبت سیستمی")]</c>, leading space trimmed here), <c>Blacklisted =
/// 3</c> (توضیح <c>[Description("  به‌طور کلی غیرمجاز")]</c>, leading spaces trimmed here) —
/// confirmed by reading the reference file directly, not guessed.
///
/// ⚠️ <b>Deliberately renamed from the reference project's bare <c>StateEnum</c> to
/// <c>WhiteBlackListState</c>.</b> A name as generic as <c>StateEnum</c> would be actively
/// misleading in our shared <c>ValueObjects</c> namespace, which already hosts many unrelated
/// "state" concepts — a future reader searching for "the STATE column enum on the allow/deny
/// list" would have no way to tell which table it belongs to from the name alone. The member
/// names themselves (<see cref="Allowed"/>/<see cref="SystemOnly"/>/<see cref="Blacklisted"/>)
/// are kept identical to the reference project — they were already PascalCase and unambiguous.
/// Cross-reference this XML doc (reference file path + original enum name) if you need to diff
/// against the source project.
/// </summary>
public enum WhiteBlackListState
{
    /// <summary>مجاز به ثبت دستی و سیستمی — equivalent to <c>Allowed = 1</c> in the reference project.</summary>
    Allowed = 1,

    /// <summary>فقط مجاز به ثبت سیستمی — equivalent to <c>SystemOnly = 2</c> in the reference project.</summary>
    SystemOnly = 2,

    /// <summary>به‌طور کلی غیرمجاز — equivalent to <c>Blacklisted = 3</c> in the reference project.</summary>
    Blacklisted = 3,
}
