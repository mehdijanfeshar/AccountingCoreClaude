using MediatR;

namespace Accounting.Application.AccountCodeInterfaces.Commands.UpdateAccountCodeInterface;

/// <summary>
/// Fully replaces the writable fields of an existing <c>TB_ACCOUNTCODE_INTERFACE</c> row (PUT
/// semantics, not PATCH) — mirrors <c>UpdateAccountCodeCommand</c>. Deliberately excludes
/// <c>ID</c>, <c>ADDUSERID</c>, <c>CREATEDDATE</c> and <c>ISDELETED</c>: identity and creation
/// audit are immutable after insert, and <c>ISDELETED</c> is owned exclusively by
/// <c>DeleteAccountCodeInterfaceCommand</c>. <c>CHANGEUSERID</c>/<c>UPDATEDDATE</c> are likewise
/// absent because the handler sources them from
/// <see cref="Accounting.Application.Common.Interfaces.ICurrentUser"/> and the server clock.
/// </summary>
/// <param name="Id">The <c>TB_ACCOUNTCODE_INTERFACE.ID</c> to update (bound from the route, never the body).</param>
/// <param name="Type">
/// TYPE column (<c>NUMBER(1)</c>, mapped as non-nullable <c>bool</c>). See
/// <see cref="Accounting.Application.AccountCodeInterfaces.Commands.CreateAccountCodeInterface.CreateAccountCodeInterfaceCommand.Type"/>
/// for the open "may actually be a multi-valued enum" note.
/// </param>
/// <param name="AccountCodeId">
/// ACCOUNTCODEID column — required FK to <c>TB_ACCOUNTCODE</c>. Reassigning this to a different
/// account code is treated as a legitimate correction, not a "move" between aggregates (unlike
/// <c>TB_VOUCHERSDETAIL.VOUCHERSHEAD_ID</c>), since this table is a standalone link table, not a
/// child of an aggregate root. A non-existent id surfaces via the same central ORA-02291 → 400
/// mapping described on the Create command.
/// </param>
public sealed record UpdateAccountCodeInterfaceCommand(
    Guid Id,
    bool Type,
    Guid AccountCodeId) : IRequest;
