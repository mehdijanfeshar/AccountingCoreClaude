using Accounting.Domain.ValueObjects;
using MediatR;

namespace Accounting.Application.WhiteAndBlackLists.Commands.CreateWhiteAndBlackListsBulk;

/// <summary>
/// Grants (or system-restricts) a set of account codes for a set of unit types in one go: the
/// handler writes the full cartesian product <c>AccountCodeIds × VahedTypeIds</c> as
/// <c>TB_WHITEANDBLACKLIST</c> rows inside a single transaction.
///
/// This mirrors the reference project's <c>AddWhiteAndBlackListCommand</c>, which is likewise a
/// cartesian bulk operation (<c>docs/centralaccount-business-reference.md</c> §24 «WhiteAndBlackList»),
/// and it is what the «افزودن دسترسی جدید» dialog needs: the user ticks several معین in one tree
/// and several نوع واحد in another.
///
/// <b>One date pair, not four.</b> The table has two date ranges and the screen only ever offers
/// one, because which pair the range lands in <i>is</i> the meaning of <paramref name="State"/>:
/// <see cref="WhiteBlackListState.Allowed"/> writes it to <c>FROMAUTHORIZEDDATE</c>/<c>TOAUTHORIZEDDATE</c>
/// and <see cref="WhiteBlackListState.SystemOnly"/> writes it to
/// <c>FROMLIMITATIONDATE</c>/<c>TOLIMITATIONDATE</c>, leaving the other pair null. That routing is
/// not invented here — it is what the old Angular screen did and what the Figma list confirms
/// (its «از/تا تاریخ محدودیت» columns are populated exactly on the rows whose «از/تا تاریخ مجاز»
/// are empty). Exposing all four fields would let a caller build a row the UI can never render.
///
/// <see cref="WhiteBlackListState.Blacklisted"/> is rejected by the validator: blacklisting is a
/// transition applied to an existing row by
/// <see cref="BlacklistWhiteAndBlackList.BlacklistWhiteAndBlackListCommand"/>, never an initial
/// state — again matching the reference project, whose add-dialog hides the third radio option.
/// </summary>
/// <param name="AccountCodeIds">The معین rows to grant. Duplicates are collapsed by the handler.</param>
/// <param name="VahedTypeIds">The <c>TB_VAHED_TYPE</c> rows to grant them for. Duplicates are collapsed by the handler.</param>
/// <param name="FromDate">Start of the range, as zero-padded <c>YYYYMMDD</c> Jalali text.</param>
/// <param name="ToDate">End of the range, as zero-padded <c>YYYYMMDD</c> Jalali text.</param>
/// <param name="State">Which kind of permission this is — and therefore which date pair is written.</param>
public sealed record CreateWhiteAndBlackListsBulkCommand(
    IReadOnlyList<Guid> AccountCodeIds,
    IReadOnlyList<Guid> VahedTypeIds,
    string? FromDate,
    string? ToDate,
    WhiteBlackListState State) : IRequest<CreateWhiteAndBlackListsBulkResult>;
