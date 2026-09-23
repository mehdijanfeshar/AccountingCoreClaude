using MediatR;

namespace Accounting.Application.WhiteAndBlackLists.Commands.BlacklistWhiteAndBlackList;

/// <summary>
/// Moves an existing allow/deny-list row to
/// <see cref="Accounting.Domain.ValueObjects.WhiteBlackListState.Blacklisted"/> — the «غیرفعال‌سازی»
/// action on the «دسترسی کدینگ حسابداری» grid.
///
/// It takes nothing but the row id on purpose. Clearing the four date columns is part of the
/// transition, not a caller choice: a blacklisted row has no authorized window and no restriction
/// window, so leaving either populated would produce a row whose dates contradict its state. This
/// matches the reference project's <c>ChangeStateToBlackListed</c>, which nulls all four
/// (<c>docs/centralaccount-business-reference.md</c> §24 «WhiteAndBlackList») — a real business
/// rule, ported deliberately rather than re-derived.
/// </summary>
/// <param name="Id">ID of the <c>TB_WHITEANDBLACKLIST</c> row to blacklist.</param>
public sealed record BlacklistWhiteAndBlackListCommand(Guid Id) : IRequest;
