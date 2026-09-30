using System.Text.Json.Serialization;
using Accounting.Application.Common.Security;
using Accounting.Application.Treasury.Queries;
using MediatR;

namespace Accounting.Application.Treasury.Queries.GetBankStatementById;

/// <summary><c>GET api/treasury/statements/{id}</c> — full detail (header + lines + summary +
/// book-only). Returns <see langword="null"/> when no row with that <c>ID</c> exists.</summary>
public sealed record GetBankStatementByIdQuery(Guid Id) : IRequest<BankStatementDto?>, IVahedScopedQuery
{
    [JsonIgnore]
    public string VahedCode { get; set; } = string.Empty;
}
