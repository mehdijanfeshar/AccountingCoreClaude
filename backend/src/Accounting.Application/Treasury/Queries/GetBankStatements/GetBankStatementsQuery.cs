using System.Text.Json.Serialization;
using Accounting.Application.Common.Security;
using Accounting.Application.Treasury.Queries;
using Accounting.Domain.ValueObjects;
using MediatR;

namespace Accounting.Application.Treasury.Queries.GetBankStatements;

/// <summary><c>GET api/treasury/statements?bankAccountId=&amp;state=&amp;pageNumber=&amp;pageSize=</c>.</summary>
public sealed record GetBankStatementsQuery(
    int PageNumber,
    int PageSize,
    Guid? BankAccountId,
    BankStatementState? State) : IRequest<BankStatementListResult>, IVahedScopedQuery
{
    [JsonIgnore]
    public string VahedCode { get; set; } = string.Empty;
}
