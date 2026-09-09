using System.Text.Json.Serialization;
using Accounting.Application.Common.Security;
using MediatR;

namespace Accounting.Application.Expenses.Commands.CreateExpense;

/// <summary>
/// Creates a new <c>TB_EXPENCE</c> row (Legacy expense-type master — a reusable expense
/// category/definition, optionally linked to a default account code and expense group). Carries
/// primitive fields only — the handler is responsible for constructing the Domain entity.
/// Returns the newly generated <see cref="Guid"/> ID.
///
/// ⚠️ The Oracle table name is misspelled (<c>TB_EXPENCE</c>). Per project convention (precedent:
/// <c>ChequeType</c> for <c>TB_CHECK_TYPE</c>), the CLR-side naming — this command's type name,
/// its folder (<c>Expenses</c>), the route (<c>/api/expenses</c>) and every parameter name below
/// — is normalised to the correct spelling <c>Expense</c>. The raw, misspelled column names
/// (<c>EXPENCECODE</c>, <c>EXPENCENAME</c>, <c>EXPENCEGROUP_ID</c>, ...) are kept exactly as-is
/// when assigning to the Domain entity in the handler — only the identifiers we own are
/// corrected, never the Oracle schema.
///
/// <c>AccountCodeId</c> is backed by a real, optional FK (<c>FK_EXPENSE_ACCOUNTCODE</c> to
/// <c>TB_ACCOUNTCODE</c>); <c>ExpenseGroupId</c> is backed by a real, optional FK
/// (<c>FK_EXPENCEGROUP</c> to <c>TB_EXPENCEGROUP</c>). Both are mapped centrally to 400 by
/// <c>UnitOfWork.SaveChangesAsync</c> on violation.
///
/// <b>UNIQUE constraint <c>UK_EXPENSE_CODE</c></b> on <c>(EXPENCECODE, VAHEDCODE)</c> is mapped
/// centrally to 409 by <c>UnitOfWork.SaveChangesAsync</c> when it triggers.
///
/// ⚠️ Its child <c>TB_EXPENCE_LINK_TAFSILI</c> is permanently embedded per team rule (any table
/// named <c>*_LINK_TAFSIL*</c> never gets independent CRUD) — no repository, no cascade, and no
/// write path for it exists anywhere in this feature.
/// </summary>
/// <param name="ExpenseCode">EXPENCECODE column (required, exactly up to 2 chars; part of <c>UK_EXPENSE_CODE</c>).</param>
/// <param name="ExpenseName">EXPENCENAME column (required, max 200 chars).</param>
/// <param name="Description">DESCRIPTION column (optional, max 100 chars).</param>
/// <param name="DefaultAmount">DEFAULTAMOUNT column (<c>NUMBER(25)</c>, optional).</param>
/// <param name="ExpenseGroupId">Optional link to <c>TB_EXPENCEGROUP</c> (<c>FK_EXPENCEGROUP</c>).</param>
/// <param name="AccountCodeId">Optional link to <c>TB_ACCOUNTCODE</c> (<c>FK_EXPENSE_ACCOUNTCODE</c>).</param>
public sealed record CreateExpenseCommand(
    string ExpenseCode,
    string ExpenseName,
    string? Description,
    decimal? DefaultAmount,
    Guid? ExpenseGroupId,
    Guid? AccountCodeId) : IRequest<Guid>, IVahedScopedCommand
{
    /// <summary>
    /// Organizational unit code (max 4 chars; part of <c>UK_EXPENSE_CODE</c>). Never bound from
    /// the request body — <see cref="JsonIgnoreAttribute"/> keeps it out of both model binding
    /// and the Swagger schema — and never trusted even if a caller manages to set it:
    /// <c>VahedScopeBehavior</c> unconditionally overwrites this with the authenticated caller's
    /// own unit code before the request reaches <c>CreateExpenseCommandHandler</c>. See
    /// <see cref="IVahedScopedCommand"/> for the full mechanism. The Oracle column itself
    /// (<c>TB_EXPENCE.VAHEDCODE</c>) is nullable, but this property never carries a null/empty
    /// value once <c>VahedScopeBehavior</c> has run — see that class's XML doc for the fail-loud
    /// guarantee.
    /// </summary>
    [JsonIgnore]
    public string VahedCode { get; set; } = string.Empty;
}
