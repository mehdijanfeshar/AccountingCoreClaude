using System.Text.Json.Serialization;
using Accounting.Application.Common.Security;
using MediatR;

namespace Accounting.Application.Expenses.Commands.DeleteExpense;

/// <summary>
/// Soft-deletes a <c>TB_EXPENCE</c> row: sets <c>ISDELETED = true</c> plus the
/// <c>CHANGEUSERID</c>/<c>UPDATEDDATE</c> audit columns. Never issues a physical DELETE — this
/// project never issues physical deletes on any entity (see CLAUDE.md). Its permanently embedded
/// child <c>TB_EXPENCE_LINK_TAFSILI</c> is NOT cascaded — no repository or write path for it
/// exists in this feature at all.
/// </summary>
/// <param name="Id">The <c>TB_EXPENCE.ID</c> to soft-delete (bound from the route).</param>
public sealed record DeleteExpenseCommand(Guid Id) : IRequest, IVahedScopedCommand
{
    /// <summary>
    /// Caller's own organizational unit, server-assigned by <c>VahedScopeBehavior</c> — never
    /// client input. Used to refuse a row belonging to another unit; see
    /// <c>VahedOwnership</c> and IDOR risk #1.
    /// </summary>
    [JsonIgnore]
    public string VahedCode { get; set; } = string.Empty;
}
