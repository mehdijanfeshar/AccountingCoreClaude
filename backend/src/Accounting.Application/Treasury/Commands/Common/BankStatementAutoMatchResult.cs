namespace Accounting.Application.Treasury.Commands.Common;

/// <summary><c>POST statements/{id}/auto-match</c> response — خزانه‌داری، بخش ۴-د.</summary>
public sealed record BankStatementAutoMatchResult(int MatchedCount, int UnmatchedCount);
