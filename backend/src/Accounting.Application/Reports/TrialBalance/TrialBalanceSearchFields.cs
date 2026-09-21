namespace Accounting.Application.Reports.TrialBalance;

/// <summary>
/// The logical field names a trial balance <c>SearchParam</c> may name. This list is the security
/// boundary of the generic filter: anything not here is refused with a 400 before SQL is built.
///
/// <para>
/// It lives in Application rather than Infrastructure because the validator has to reject unknown
/// names, and Application may not reference Infrastructure. The SQL expression each name maps to
/// is Infrastructure's business — it picks the column, the caller only picks the name.
/// <c>TrialBalanceSearchFieldMappingTests</c> fails if the two sides ever disagree, so adding a
/// name here without mapping it (or the reverse) cannot ship.
/// </para>
/// </summary>
public static class TrialBalanceSearchFields
{
    /// <summary>The account code at the requested level (گروه/کل/معین).</summary>
    public const string Code = "code";

    /// <summary>The account name at the requested level.</summary>
    public const string Description = "description";

    /// <summary>
    /// Every allowed name, compared case-insensitively so <c>Code</c> and <c>code</c> both work —
    /// a caller should not have to guess our casing.
    /// </summary>
    public static readonly IReadOnlySet<string> All =
        new HashSet<string>(StringComparer.OrdinalIgnoreCase) { Code, Description };
}
