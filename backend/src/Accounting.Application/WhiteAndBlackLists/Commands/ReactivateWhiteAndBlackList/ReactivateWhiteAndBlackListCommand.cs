using Accounting.Domain.ValueObjects;
using MediatR;

namespace Accounting.Application.WhiteAndBlackLists.Commands.ReactivateWhiteAndBlackList;

/// <summary>
/// Brings a blacklisted row back into service — the «فعال سازی مجدد» dialog. The caller picks
/// which kind of permission it becomes (<see cref="WhiteBlackListState.Allowed"/> or
/// <see cref="WhiteBlackListState.SystemOnly"/>) and supplies the new date range, because
/// blacklisting cleared the old one and there is nothing to restore.
///
/// Like the bulk grant, this takes <b>one</b> date pair and lets
/// <paramref name="State"/> decide which pair of columns it lands in; see
/// <see cref="CreateWhiteAndBlackListsBulk.CreateWhiteAndBlackListsBulkCommand"/> for why that
/// routing is the meaning of the state rather than a caller choice. It mirrors the reference
/// project's <c>ReActive</c>, which likewise takes the dates and the state together.
/// </summary>
/// <param name="Id">ID of the <c>TB_WHITEANDBLACKLIST</c> row to reactivate.</param>
/// <param name="State">The state to return to. <see cref="WhiteBlackListState.Blacklisted"/> is rejected.</param>
/// <param name="FromDate">Start of the new range, as zero-padded <c>YYYYMMDD</c> Jalali text.</param>
/// <param name="ToDate">End of the new range, as zero-padded <c>YYYYMMDD</c> Jalali text.</param>
public sealed record ReactivateWhiteAndBlackListCommand(
    Guid Id,
    WhiteBlackListState State,
    string? FromDate,
    string? ToDate) : IRequest;
