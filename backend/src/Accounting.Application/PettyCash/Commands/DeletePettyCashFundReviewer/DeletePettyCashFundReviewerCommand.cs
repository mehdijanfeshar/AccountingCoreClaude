using System.Text.Json.Serialization;
using Accounting.Application.Common.Security;
using MediatR;

namespace Accounting.Application.PettyCash.Commands.DeletePettyCashFundReviewer;

/// <summary>
/// <c>POST api/petty-cash/funds/{fundId}/reviewers/{id}/delete</c> — soft-deletes a
/// <c>TB_PC_REVIEWER</c> row. <see cref="FundId"/> comes from the route only to scope/validate the
/// path (the reviewer row must actually belong to that fund); the row is looked up and mutated by
/// <see cref="Id"/> alone.
/// </summary>
public sealed record DeletePettyCashFundReviewerCommand(Guid FundId, Guid Id) : IRequest, IVahedScopedCommand
{
    [JsonIgnore]
    public string VahedCode { get; set; } = string.Empty;
}
