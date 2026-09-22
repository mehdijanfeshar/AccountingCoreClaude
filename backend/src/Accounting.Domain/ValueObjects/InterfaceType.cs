namespace Accounting.Domain.ValueObjects;

/// <summary>
/// Opening/closing interface-voucher kind — maps the numeric value of column
/// <c>TB_ACCOUNTCODE_INTERFACE.TYPE</c> (previously, incorrectly, non-nullable <c>bool</c>; see
/// open risk #2 in <c>CLAUDE.md</c> and <c>docs/centralaccount-business-reference.md</c> §24-1) to
/// this enum.
///
/// Equivalent to enum <c>InterfaceType</c> in the reference project
/// (<c>D:\CentralAccount\Tamin.Core\Entities\AccountCodeIntefaces\InterfaceType.cs</c> — note the
/// typo "Intefaces" in their folder name, preserved here only as a citation, not copied into our
/// path): <c>openVoucher = 1</c> (توضیح <c>[Description("افتتاحیه")]</c>), <c>closeVoucher = 2</c>
/// (توضیح <c>[Description("اختتامیه")]</c>) — confirmed by reading the reference file directly,
/// not guessed.
///
/// Member names below (<see cref="OpenVoucher"/>/<see cref="CloseVoucher"/>) are PascalCase
/// translations of the reference project's camelCase <c>openVoucher</c>/<c>closeVoucher</c>,
/// following .NET naming conventions — the underlying numeric values are identical.
/// </summary>
public enum InterfaceType
{
    /// <summary>افتتاحیه (opening) — equivalent to <c>openVoucher = 1</c> in the reference project.</summary>
    OpenVoucher = 1,

    /// <summary>اختتامیه (closing) — equivalent to <c>closeVoucher = 2</c> in the reference project.</summary>
    CloseVoucher = 2,
}
