namespace Accounting.Application.WhiteAndBlackLists.Commands.CreateWhiteAndBlackListsBulk;

/// <summary>
/// Outcome of a bulk grant. Both numbers are reported because "nothing happened" and "everything
/// happened" are equally plausible results of the same click: re-running a grant that is already
/// in place is a normal thing for a user to do, and answering it with a 409 would be wrong.
/// </summary>
/// <param name="Created">Rows actually inserted.</param>
/// <param name="Skipped">
/// Combinations dropped because an identical row (same account, unit type and authorized-date
/// pair — the <c>UK_WHITEANDBLACKLIST</c> key) already existed and was not deleted.
/// </param>
/// <param name="CreatedIds">IDs of the inserted rows, in insertion order.</param>
public sealed record CreateWhiteAndBlackListsBulkResult(
    int Created,
    int Skipped,
    IReadOnlyList<Guid> CreatedIds);
