using MediatR;

namespace Accounting.Application.AccountCodeInterfaces.Commands.CreateAccountCodeInterface;

/// <summary>
/// Creates a new <c>TB_ACCOUNTCODE_INTERFACE</c> row (a legacy interface-flag link on an
/// account code). Carries primitive fields only — the handler is responsible for constructing
/// the Domain entity. Returns the newly generated <see cref="Guid"/> ID.
/// </summary>
/// <param name="Type">
/// TYPE column (<c>NUMBER(1)</c>, mapped as non-nullable <c>bool</c>). NOTE: this column is
/// suspected to actually be a multi-valued enum (≈ <c>InterfaceType</c> 1..2) rather than a
/// true boolean, per the open decision recorded in CLAUDE.md. Modeled as <c>bool</c> exactly as
/// the current Domain entity declares it — fixing the underlying type is a separate,
/// out-of-scope task and was deliberately not guessed here.
/// </param>
/// <param name="AccountCodeId">
/// ACCOUNTCODEID column — required FK to <c>TB_ACCOUNTCODE</c> (constraint
/// <c>FK_INTRFACE_ACCCOUNTCODE</c>). No pre-check is performed here: an id that does not
/// reference an existing <c>TB_ACCOUNTCODE</c> row surfaces as an Oracle ORA-02291, which
/// <c>UnitOfWork.SaveChangesAsync</c> already translates centrally into a
/// <see cref="Accounting.Application.Common.Exceptions.ForeignKeyViolationException"/> → 400.
/// </param>
public sealed record CreateAccountCodeInterfaceCommand(
    bool Type,
    Guid AccountCodeId) : IRequest<Guid>;
