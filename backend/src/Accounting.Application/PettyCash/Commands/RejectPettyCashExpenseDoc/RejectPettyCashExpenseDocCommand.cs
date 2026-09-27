using System.Text.Json.Serialization;
using Accounting.Application.Common.Security;
using MediatR;

namespace Accounting.Application.PettyCash.Commands.RejectPettyCashExpenseDoc;

/// <summary>
/// <c>POST api/petty-cash/expense-docs/{id}/reject</c> — moves a صورت‌هزینه from
/// <see cref="Accounting.Domain.ValueObjects.PettyCashDocState.PendingReview"/> to
/// <see cref="Accounting.Domain.ValueObjects.PettyCashDocState.Rejected"/> (terminal, no ترمیم).
/// </summary>
public sealed record RejectPettyCashExpenseDocCommand(Guid Id, string? Note) : IRequest, IVahedScopedCommand
{
    [JsonIgnore]
    public string VahedCode { get; set; } = string.Empty;
}
