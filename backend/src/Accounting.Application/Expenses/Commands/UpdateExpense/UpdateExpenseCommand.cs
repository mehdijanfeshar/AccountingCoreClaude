using System.Text.Json.Serialization;
using Accounting.Application.Common.Security;
using MediatR;

namespace Accounting.Application.Expenses.Commands.UpdateExpense;

/// <summary>
/// Fully replaces the writable fields of an existing <c>TB_EXPENCE</c> row (PUT semantics, not
/// PATCH) — mirrors <c>UpdateWorkShopCommand</c>. Deliberately excludes <c>ID</c>,
/// <c>ADDUSERID</c>, <c>CREATEDDATE</c> and <c>ISDELETED</c>. <c>CHANGEUSERID</c>/<c>UPDATEDDATE</c>
/// are likewise absent because the handler sources them from
/// <see cref="Accounting.Application.Common.Interfaces.ICurrentUser"/> and the server clock.
///
/// Field-by-field documentation is intentionally not repeated here — every field (including
/// <c>Id</c>, bound from the route and never the body) has the exact same column/type/length
/// meaning as the identically-named parameter on
/// <see cref="Accounting.Application.Expenses.Commands.CreateExpense.CreateExpenseCommand"/>; see
/// that command's XML doc for the misspelled-table-name/naming-normalisation note and the
/// FK/UNIQUE flags.
///
/// ⚠️ <b>Scope note:</b> <see cref="IVahedScopedCommand"/> here only guarantees that
/// <c>VAHEDCODE</c> cannot be *changed* to an arbitrary unit by the caller. It does
/// <b>not</b> check whether the caller is allowed to touch this particular row in the first
/// place — record-ownership verification on Update is explicitly out of scope for this pass, by
/// project-owner decision. The IDOR risk on direct-by-id access therefore remains open for
/// Update; only the "what unit does this row end up in" half of the problem is closed here.
/// </summary>
public sealed record UpdateExpenseCommand(
    Guid Id,
    string ExpenseCode,
    string ExpenseName,
    string? Description,
    decimal? DefaultAmount,
    Guid? ExpenseGroupId,
    Guid? AccountCodeId) : IRequest, IVahedScopedCommand
{
    /// <summary>
    /// Organizational unit code (max 4 chars). Never bound from the request body —
    /// <see cref="JsonIgnoreAttribute"/> keeps it out of both model binding and the Swagger
    /// schema — and never trusted even if a caller manages to set it: <c>VahedScopeBehavior</c>
    /// unconditionally overwrites this with the authenticated caller's own unit code before the
    /// request reaches <c>UpdateExpenseCommandHandler</c>. See <see cref="IVahedScopedCommand"/>
    /// for the full mechanism, and the scope note above for what this does <b>not</b> cover.
    /// </summary>
    [JsonIgnore]
    public string VahedCode { get; set; } = string.Empty;
}
