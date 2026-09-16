using Accounting.Domain.ValueObjects;
using MediatR;

namespace Accounting.Application.AccountCodeInterfaces.Commands.CreateAccountCodeInterface;

/// <summary>
/// Creates a new <c>TB_ACCOUNTCODE_INTERFACE</c> row (a legacy interface-flag link on an
/// account code). Carries primitive fields only — the handler is responsible for constructing
/// the Domain entity. Returns the newly generated <see cref="Guid"/> ID.
/// </summary>
/// <param name="Type">
/// TYPE column (<c>NUMBER(1)</c>, mapped as non-nullable <see cref="InterfaceType"/>). Resolved
/// per <c>docs/centralaccount-business-reference.md</c> §24-1 (phase 27 batch 3) — previously an
/// incorrect <c>bool</c>; see the historical note preserved in the open risk #2 entry of
/// CLAUDE.md.
/// </param>
/// <param name="AccountCodeId">
/// ACCOUNTCODEID column — required FK to <c>TB_ACCOUNTCODE</c> (constraint
/// <c>FK_INTRFACE_ACCCOUNTCODE</c>). No pre-check is performed here: an id that does not
/// reference an existing <c>TB_ACCOUNTCODE</c> row surfaces as an Oracle ORA-02291, which
/// <c>UnitOfWork.SaveChangesAsync</c> already translates centrally into a
/// <see cref="Accounting.Application.Common.Exceptions.ForeignKeyViolationException"/> → 400.
/// </param>
public sealed record CreateAccountCodeInterfaceCommand(
    InterfaceType Type,
    Guid AccountCodeId) : IRequest<Guid>;
