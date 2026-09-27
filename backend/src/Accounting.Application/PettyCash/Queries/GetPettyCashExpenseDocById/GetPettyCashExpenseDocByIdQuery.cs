using System.Text.Json.Serialization;
using Accounting.Application.Common.Security;
using MediatR;

namespace Accounting.Application.PettyCash.Queries.GetPettyCashExpenseDocById;

/// <summary>
/// <c>GET api/petty-cash/expense-docs/{id}</c>. Implements <see cref="IVahedScoped"/>
/// unconditionally, per the project-wide "every Get...ByIdQuery is unit-scoped" rule
/// (<c>VahedScopeConventionTests.EveryGetByIdQuery_ImplementsIVahedScoped</c>).
/// </summary>
public sealed record GetPettyCashExpenseDocByIdQuery(Guid Id) : IRequest<PettyCashExpenseDocDto?>, IVahedScoped
{
    [JsonIgnore]
    public string VahedCode { get; set; } = string.Empty;
}
