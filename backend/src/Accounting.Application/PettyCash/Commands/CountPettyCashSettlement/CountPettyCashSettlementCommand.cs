using System.Text.Json.Serialization;
using Accounting.Application.Common.Security;
using MediatR;

namespace Accounting.Application.PettyCash.Commands.CountPettyCashSettlement;

/// <summary>
/// <c>POST api/petty-cash/funds/{fundId}/settlement/count</c> — بخش ۳-ب
/// (<c>docs/tankhah-khazaneh-module.md</c> section 9). Records the physical cash count for the
/// fund's currently-open Draft period, creating that period row if none exists yet. Returns the
/// period's <c>ID</c>.
///
/// <b>No role restriction here by design.</b> §۹ names a role only for <c>finalize</c>
/// ("فقط SeniorAccountant") — it says nothing about who may record a count, and inventing a
/// restriction not asked for would be exactly the kind of business rule this project's handlers
/// are told not to copy/guess. Any authenticated caller scoped to the fund's own unit (enforced
/// by <c>VahedScopeBehavior</c>/<see cref="IVahedScopedCommand"/>, same as every other write in
/// this module) may record a count; only <c>finalize</c> is role-gated.
/// </summary>
public sealed record CountPettyCashSettlementCommand(Guid FundId, decimal CountedBalance) : IRequest<Guid>, IVahedScopedCommand
{
    [JsonIgnore]
    public string VahedCode { get; set; } = string.Empty;
}
