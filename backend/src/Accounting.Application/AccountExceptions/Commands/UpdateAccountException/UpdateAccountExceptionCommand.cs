using MediatR;

namespace Accounting.Application.AccountExceptions.Commands.UpdateAccountException;

/// <summary>
/// Fully replaces the writable fields of an existing <c>TB_ACCOUNTEXCEPTION</c> row (PUT
/// semantics, not PATCH) — mirrors <c>UpdateAccountCodeCommand</c>. Deliberately excludes
/// <c>ID</c>, <c>ADDUSERID</c>, <c>CREATEDDATE</c> and <c>ISDELETED</c>.
/// </summary>
/// <param name="Id">The <c>TB_ACCOUNTEXCEPTION.ID</c> to update (bound from the route, never the body).</param>
/// <param name="AccountCoeId">
/// ACCOUNTCOE_ID column — required FK to <c>TB_ACCOUNTCODE</c>. See
/// <see cref="Accounting.Application.AccountExceptions.Commands.CreateAccountException.CreateAccountExceptionCommand.AccountCoeId"/>
/// for the "keep the typo" note. Reassigning this is treated as a legitimate correction (this
/// table is a standalone rule link, not a child of an aggregate root).
/// </param>
/// <param name="VahedTypeId">VAHEDTYPE_ID column — required FK to <c>TB_VAHED_TYPE</c>.</param>
public sealed record UpdateAccountExceptionCommand(
    Guid Id,
    Guid AccountCoeId,
    Guid VahedTypeId) : IRequest;
