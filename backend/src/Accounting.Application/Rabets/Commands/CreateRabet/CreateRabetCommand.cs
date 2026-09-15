using MediatR;

namespace Accounting.Application.Rabets.Commands.CreateRabet;

/// <summary>
/// Creates a new <c>TB_RABET</c> row (Legacy account-code/rabet-type link). Carries primitive
/// fields only — the handler is responsible for constructing the Domain entity. Returns the
/// newly generated <see cref="Guid"/> ID.
///
/// Both fields are optional <see cref="Guid"/> foreign keys with no further surface-level
/// constraint to enforce (mirroring how <c>CreateAccountCodeCommand.ParentId</c> is left
/// unvalidated) — the combination is protected by the real DB constraint <c>UK_RABET</c>
/// instead, mapped centrally by <c>UnitOfWork.SaveChangesAsync</c> to 409. FK violations on
/// either id (row does not exist) are likewise mapped centrally to 400 — see
/// <c>Accounting.Api.Controllers.RabetsController</c> XML doc.
/// </summary>
/// <param name="RabetTypeId">Optional link to <c>TB_RABET_TYPE</c> (<c>FK_RABET_TYPE</c>).</param>
/// <param name="AccountCodeId">Optional link to <c>TB_ACCOUNTCODE</c> (<c>FK_RABET_ACCOUNTCODE</c>).</param>
public sealed record CreateRabetCommand(
    Guid? RabetTypeId,
    Guid? AccountCodeId) : IRequest<Guid>;
