using System.Text.Json.Serialization;
using Accounting.Application.Common.Security;
using MediatR;

namespace Accounting.Application.PettyCash.Commands.BulkApprovePettyCashExpenseDocs;

/// <summary>
/// <c>POST api/petty-cash/expense-docs/bulk-approve</c> — applies the same transition as
/// <c>ApprovePettyCashExpenseDocCommand</c> to every id in <see cref="Ids"/>, in one transaction,
/// all-or-nothing: if even one id fails its own Approve check (not found, not an active reviewer,
/// self-review conflict, or not <see cref="Accounting.Domain.ValueObjects.PettyCashDocState.PendingReview"/>),
/// the whole request fails with <b>409</b> and none of the ids in the batch is approved — see
/// <see cref="Accounting.Application.Common.Exceptions.PettyCashBulkApproveConflictException"/>.
/// </summary>
public sealed record BulkApprovePettyCashExpenseDocsCommand(IReadOnlyList<Guid> Ids) : IRequest, IVahedScopedCommand
{
    [JsonIgnore]
    public string VahedCode { get; set; } = string.Empty;
}
