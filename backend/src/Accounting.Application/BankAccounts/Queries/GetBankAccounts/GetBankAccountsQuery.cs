using Accounting.Application.Common;
using MediatR;

namespace Accounting.Application.BankAccounts.Queries.GetBankAccounts;

/// <summary>
/// Returns a page of <c>TB_ACCOUNT</c> rows projected to <see cref="BankAccountDto"/>. Only
/// non-deleted rows (<c>ISDELETED != true</c>) are included.
/// </summary>
/// <param name="PageNumber">1-based page number.</param>
/// <param name="PageSize">Page size, capped by <see cref="GetBankAccountsQueryValidator.MaxPageSize"/>.</param>
public sealed record GetBankAccountsQuery(int PageNumber, int PageSize) : IRequest<PagedResult<BankAccountDto>>;
