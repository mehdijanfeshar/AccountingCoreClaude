using System.Text.Json.Serialization;
using Accounting.Application.Common.Security;
using MediatR;

namespace Accounting.Application.PettyCash.Commands.UpsertPettyCashFundReviewer;

/// <summary>
/// <c>POST api/petty-cash/funds/{fundId}/reviewers</c> — creates a new <c>TB_PC_REVIEWER</c> row
/// for (<see cref="FundId"/>, <see cref="ReviewerUserId"/>) if none exists yet (including none
/// soft-deleted), or reactivates/renames the existing one — <c>UK_PC_REVIEWER</c> is unique on
/// that pair. Unlike a fund's own definition this is not 1:1 with the fund, so the response is the
/// reviewer row's own <c>Id</c>, not just an echo of <see cref="FundId"/>.
/// </summary>
public sealed record UpsertPettyCashFundReviewerCommand(
    Guid FundId,
    string ReviewerUserId,
    string? ReviewerName) : IRequest<Guid>, IVahedScopedCommand
{
    [JsonIgnore]
    public string VahedCode { get; set; } = string.Empty;
}
