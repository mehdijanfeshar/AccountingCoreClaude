namespace Accounting.Application.PettyCash.Queries;

/// <summary>Response shape for one <c>TB_PC_REVIEWER</c> row — never the bare entity (CLAUDE.md rule 6).</summary>
public sealed record PettyCashFundReviewerDto(
    Guid Id,
    Guid FundId,
    string ReviewerUserId,
    string? ReviewerName);
