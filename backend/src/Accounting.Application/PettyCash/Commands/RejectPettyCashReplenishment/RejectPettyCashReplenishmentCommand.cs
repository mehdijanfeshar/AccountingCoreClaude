using System.Text.Json.Serialization;
using Accounting.Application.Common.Security;
using Accounting.Domain.ValueObjects;
using MediatR;

namespace Accounting.Application.PettyCash.Commands.RejectPettyCashReplenishment;

/// <summary>
/// <c>POST api/petty-cash/replenishments/{id}/reject</c> — moves a ترمیم from either
/// <see cref="PettyCashReplenishmentState.PendingFinanceManager"/> or
/// <see cref="PettyCashReplenishmentState.PendingTreasurer"/> to
/// <see cref="PettyCashReplenishmentState.Rejected"/> (پایانی). Caller must hold
/// <see cref="Accounting.Domain.ValueObjects.PettyCashRole.FinanceManager"/> or
/// <see cref="Accounting.Domain.ValueObjects.PettyCashRole.Treasurer"/> for the ترمیم's fund. Every
/// <c>TB_CHARGE_LINK_COST</c> row linked to this ترمیم is soft-deleted so its صورت‌هزینه documents
/// become replenishable again (بخش ۳-الف، <c>docs/tankhah-khazaneh-module.md</c>).
/// </summary>
public sealed record RejectPettyCashReplenishmentCommand(Guid Id, string? Note) : IRequest, IVahedScopedCommand
{
    [JsonIgnore]
    public string VahedCode { get; set; } = string.Empty;
}
