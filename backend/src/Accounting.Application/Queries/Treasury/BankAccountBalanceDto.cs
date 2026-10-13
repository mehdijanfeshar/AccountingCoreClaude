namespace Accounting.Application.Queries.Treasury;

/// <summary><c>GET api/treasury/bank-accounts/{id}/balance</c> response.</summary>
public sealed record BankAccountBalanceDto(Guid BankAccountId, decimal Balance);
