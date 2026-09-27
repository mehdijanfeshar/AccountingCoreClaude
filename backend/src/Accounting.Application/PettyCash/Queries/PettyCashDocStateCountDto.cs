using Accounting.Domain.ValueObjects;

namespace Accounting.Application.PettyCash.Queries;

/// <summary>
/// One entry of the <c>stateCounts</c> array returned alongside the کارتابل list
/// (<c>docs/tankhah-khazaneh-module.md</c> §5). Computed with the <c>fundId</c>/<c>search</c>
/// filters applied but the <c>state</c>/<c>states</c> filter deliberately IGNORED, so a single
/// list request can populate every tab badge at once instead of the caller issuing one request
/// per tab.
/// </summary>
public sealed record PettyCashDocStateCountDto(PettyCashDocState State, int Count);
