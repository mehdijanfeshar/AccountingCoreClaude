using System.Text.Json.Serialization;
using Accounting.Application.Common.Security;
using MediatR;

namespace Accounting.Application.WorkShops.Commands.DeleteWorkShop;

/// <summary>
/// Soft-deletes a <c>TB_WORKSHOP</c> row: sets <c>ISDELETED = true</c> plus the
/// <c>CHANGEUSERID</c>/<c>UPDATEDDATE</c> audit columns. Never issues a physical DELETE — this
/// project never issues physical deletes on any entity (see CLAUDE.md).
/// </summary>
/// <param name="Id">The <c>TB_WORKSHOP.ID</c> to soft-delete (bound from the route).</param>
public sealed record DeleteWorkShopCommand(Guid Id) : IRequest, IVahedScopedCommand
{
    /// <summary>
    /// Caller's own organizational unit, server-assigned by <c>VahedScopeBehavior</c>. A delete
    /// never writes this column — it is here so the lookup can refuse to hand back another
    /// unit's row, which until now a caller holding any valid <c>ID</c> could delete outright.
    /// </summary>
    [JsonIgnore]
    public string VahedCode { get; set; } = string.Empty;
}
