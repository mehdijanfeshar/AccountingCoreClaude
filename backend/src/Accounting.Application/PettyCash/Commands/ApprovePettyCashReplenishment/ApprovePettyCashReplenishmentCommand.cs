using System.Text.Json.Serialization;
using Accounting.Application.Common.Security;
using Accounting.Domain.ValueObjects;
using MediatR;

namespace Accounting.Application.PettyCash.Commands.ApprovePettyCashReplenishment;

/// <summary>
/// <c>POST api/petty-cash/replenishments/{id}/approve</c> — moves a ترمیم from
/// <see cref="PettyCashReplenishmentState.PendingFinanceManager"/> to
/// <see cref="PettyCashReplenishmentState.PendingTreasurer"/>. Caller must hold
/// <see cref="Accounting.Domain.ValueObjects.PettyCashRole.FinanceManager"/> for the ترمیم's fund
/// and must not be its own creator (بخش ۳-الف، <c>docs/tankhah-khazaneh-module.md</c>: «فقط نقش
/// FinanceManager همان تنخواه؛ ≠ ایجادکننده»).
/// </summary>
public sealed record ApprovePettyCashReplenishmentCommand(Guid Id) : IRequest, IVahedScopedCommand
{
    [JsonIgnore]
    public string VahedCode { get; set; } = string.Empty;
}
