using MediatR;

namespace Accounting.Application.AccountExceptions.Commands.CreateAccountException;

/// <summary>
/// Creates a new <c>TB_ACCOUNTEXCEPTION</c> row (a legacy per-unit-type exception rule on an
/// account code). Carries primitive fields only — the handler is responsible for constructing
/// the Domain entity. Returns the newly generated <see cref="Guid"/> ID.
/// </summary>
/// <param name="AccountCoeId">
/// ACCOUNTCOE_ID column — required FK to <c>TB_ACCOUNTCODE</c> (constraint
/// <c>FK_EXCEPTION_ACCOUNTCODE</c>). NOTE: the real Oracle column name has a typo
/// ("ACCOUNTCOE" instead of "ACCOUNTCODE") — this is intentionally preserved here rather than
/// silently corrected, matching the Domain entity's <c>ACCOUNTCOE_ID</c> property name. No
/// pre-check is performed: an id that does not reference an existing <c>TB_ACCOUNTCODE</c> row
/// surfaces as an Oracle ORA-02291, already translated centrally by
/// <c>UnitOfWork.SaveChangesAsync</c> into a
/// <see cref="Accounting.Application.Common.Exceptions.ForeignKeyViolationException"/> → 400.
/// </param>
/// <param name="VahedTypeId">
/// VAHEDTYPE_ID column — required FK to <c>TB_VAHED_TYPE</c> (constraint
/// <c>FK_ACCOUNTEXCEPTION_VAHEDTYPE</c>). Same no-pre-check / central ORA-02291 → 400 mapping
/// applies.
/// </param>
public sealed record CreateAccountExceptionCommand(
    Guid AccountCoeId,
    Guid VahedTypeId) : IRequest<Guid>;
