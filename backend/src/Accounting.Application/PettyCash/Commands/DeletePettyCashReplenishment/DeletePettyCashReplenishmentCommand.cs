using System.Text.Json.Serialization;
using Accounting.Application.Common.Security;
using MediatR;

namespace Accounting.Application.PettyCash.Commands.DeletePettyCashReplenishment;

/// <summary>
/// <c>POST api/petty-cash/replenishments/{id}/delete</c> — soft-deletes a ترمیم. Only allowed
/// while it is <see cref="Accounting.Domain.ValueObjects.PettyCashReplenishmentState.Draft"/>.
/// Every linked <c>TB_CHARGE_LINK_COST</c> row (and the underlying <c>TB_CHARGEANDCOST_HEAD</c>)
/// is soft-deleted too, so the صورت‌هزینه documents become replenishable again — بخش ۳-الف
/// (<c>docs/tankhah-khazaneh-module.md</c>).
/// </summary>
public sealed record DeletePettyCashReplenishmentCommand(Guid Id) : IRequest, IVahedScopedCommand
{
    [JsonIgnore]
    public string VahedCode { get; set; } = string.Empty;
}
