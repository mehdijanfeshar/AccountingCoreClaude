using System.Text.Json.Serialization;
using Accounting.Application.Common.Security;
using Accounting.Domain.ValueObjects;
using MediatR;

namespace Accounting.Application.PettyCash.Commands.SubmitPettyCashReplenishment;

/// <summary>
/// <c>POST api/petty-cash/replenishments/{id}/submit</c> — moves a ترمیم from
/// <see cref="PettyCashReplenishmentState.Draft"/> to
/// <see cref="PettyCashReplenishmentState.PendingFinanceManager"/>. Takes no body. Caller must
/// hold <see cref="Accounting.Domain.ValueObjects.PettyCashRole.FinanceManager"/> or
/// <see cref="Accounting.Domain.ValueObjects.PettyCashRole.Treasurer"/> for the ترمیم's fund —
/// same rule as Create (بخش ۳-الف، <c>docs/tankhah-khazaneh-module.md</c>).
/// </summary>
public sealed record SubmitPettyCashReplenishmentCommand(Guid Id) : IRequest, IVahedScopedCommand
{
    [JsonIgnore]
    public string VahedCode { get; set; } = string.Empty;
}
