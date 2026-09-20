namespace Accounting.Application.IdentityHeads.Commands.Common;

/// <summary>
/// One fixed-value item travelling with its شناسنامه on create/update.
///
/// Deliberately carries <b>only</b> the subgroup it belongs to and the value. Everything else on
/// the <c>TB_IDENTITYFIXITEMS</c> row — its own ID, <c>IDENTITYHEAD_ID</c>, <c>VAHEDCODE</c>,
/// <c>YEAR</c> and the whole audit trail — is derived server-side from the head being written in
/// the same call and is never accepted from the caller. Mirrors
/// <c>BankAccountTafsiliLinkInput</c>.
///
/// <para>
/// <c>IdentitySubGroupId</c> is therefore also the item's identity as far as a caller is
/// concerned, which is what the update path matches on — and it lines up with the real UNIQUE
/// constraint <c>AK_AK_IDENTYFIXITEMS_IDENTYFI</c> on
/// <c>(IDENTITYHEAD_ID, IDENTITYSUBGRPS_ID, VAHEDCODE, YEAR)</c>: one value per subgroup per
/// شناسنامه.
/// </para>
/// </summary>
/// <param name="IdentitySubGroupId">
/// IDENTITYSUBGRPS_ID — which fixed subgroup this value belongs to.
/// ⚠️ The server does <b>not</b> verify that this subgroup actually belongs to the head's group,
/// nor that it is a fixed (rather than variable) one; <c>FK_IDENTYFI_IDENTYSU</c> only proves the
/// subgroup exists. See <c>IdentityHeadsController</c> for why that check was not invented here.
/// </param>
/// <param name="Value">
/// FIXITEMS_VALUE — free text in Legacy (<c>VARCHAR2</c>, nullable). The subgroup's own
/// <c>SUBGRPS_TYPE</c>/<c>SUBGRPS_LEN</c> describe what it is meant to hold, but Legacy does not
/// enforce either and neither do we.
/// </param>
public sealed record IdentityHeadFixItemInput(
    Guid IdentitySubGroupId,
    string? Value);
