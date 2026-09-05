using MediatR;

namespace Accounting.Application.BankAccounts.Commands.DeleteBankAccount;

/// <summary>
/// Soft-deletes a <c>TB_ACCOUNT</c> row: sets <c>ISDELETED = true</c> plus the
/// <c>CHANGEUSERID</c>/<c>UPDATEDDATE</c> audit columns. Never issues a physical DELETE — this
/// project never issues physical deletes on any entity (see CLAUDE.md). Note this does NOT
/// cascade to the permanently-embedded child <c>TB_ACCOUNT_LINK_TAFSILI</c> table — no
/// repository/write path exists for it in this project at all (per team rule, every
/// <c>*_LINK_TAFSIL*</c> table stays embedded and untouched).
/// </summary>
/// <param name="Id">The <c>TB_ACCOUNT.ID</c> to soft-delete (bound from the route).</param>
public sealed record DeleteBankAccountCommand(Guid Id) : IRequest;
