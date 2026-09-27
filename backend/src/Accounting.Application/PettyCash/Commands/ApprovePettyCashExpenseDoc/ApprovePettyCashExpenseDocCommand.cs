using System.Text.Json.Serialization;
using Accounting.Application.Common.Security;
using MediatR;

namespace Accounting.Application.PettyCash.Commands.ApprovePettyCashExpenseDoc;

/// <summary>
/// <c>POST api/petty-cash/expense-docs/{id}/approve</c> — moves a صورت‌هزینه from
/// <see cref="Accounting.Domain.ValueObjects.PettyCashDocState.PendingReview"/> to
/// <see cref="Accounting.Domain.ValueObjects.PettyCashDocState.Approved"/> ("منتظر ترمیم").
/// </summary>
public sealed record ApprovePettyCashExpenseDocCommand(Guid Id, string? Note) : IRequest, IVahedScopedCommand
{
    [JsonIgnore]
    public string VahedCode { get; set; } = string.Empty;
}
