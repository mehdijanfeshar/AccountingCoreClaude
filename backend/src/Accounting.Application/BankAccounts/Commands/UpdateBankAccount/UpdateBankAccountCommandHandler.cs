using Accounting.Application.Common.Exceptions;
using Accounting.Application.BankAccounts.Commands.Common;
using Accounting.Application.Common.Interfaces;
using Accounting.Domain.Entity;
using MediatR;

namespace Accounting.Application.BankAccounts.Commands.UpdateBankAccount;

/// <summary>
/// Loads the existing <see cref="Accounting.Domain.Entity.TB_ACCOUNT"/> row via
/// <see cref="IBankAccountRepository.GetForUpdateAsync"/> (change-tracked), overwrites every
/// writable field from the command, stamps audit columns, and owns the transaction boundary by
/// calling <see cref="IUnitOfWork.SaveChangesAsync"/> exactly once. Throws
/// <see cref="NotFoundException"/> — mapped to 404 by <c>GlobalExceptionHandler</c> — when the
/// row does not exist or is already soft-deleted. <c>ISDELETED</c> is <c>bool?</c> on this table,
/// so both <see langword="false"/> and <see langword="null"/> are treated as "not deleted" — only
/// an explicit <see langword="true"/> triggers 404, consistent with the <c>ISDELETED != true</c>
/// filter used by the read side. <c>CHANGEUSERID</c> is sourced from <see cref="ICurrentUser"/>
/// (the authenticated caller) — never from the request.
///
/// <c>request.VahedCode</c> is likewise unforgeable, enforced one layer earlier: by the time
/// this handler runs, <c>VahedScopeBehavior</c> has already overwritten it with
/// <see cref="ICurrentUser.VahedCode"/>, so this handler simply maps it onto
/// <c>TB_ACCOUNT.VAHEDCODE</c> at face value — see <c>UpdateBankAccountCommand</c> XML doc for
/// the explicit scope note on what this does and does not cover.
/// </summary>
public sealed class UpdateBankAccountCommandHandler : IRequestHandler<UpdateBankAccountCommand>
{
    private readonly IBankAccountRepository _bankAccountRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUser _currentUser;

    public UpdateBankAccountCommandHandler(
        IBankAccountRepository bankAccountRepository,
        IUnitOfWork unitOfWork,
        ICurrentUser currentUser)
    {
        _bankAccountRepository = bankAccountRepository;
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
    }

    public async Task Handle(UpdateBankAccountCommand request, CancellationToken cancellationToken)
    {
        var entity = await _bankAccountRepository.GetForUpdateAsync(request.Id, request.VahedCode, cancellationToken);

        if (entity is null || entity.ISDELETED == true)
        {
            throw new NotFoundException("BankAccount", request.Id);
        }

        entity.ACCOUNTNUMBER = request.AccountNumber;
        entity.ACCOUNTHOLDER = request.AccountHolder;
        entity.CARDNUMBER = request.CardNumber;
        entity.SHEBANUMBER = request.ShebaNumber;
        entity.FIRSTAMOUNT = request.FirstAmount;
        entity.BANK_ID = request.BankId;
        entity.BRANCH_ID = request.BranchId;
        entity.ACCOUNTTYPE_ID = request.AccountTypeId;
        entity.ACCOUNTCODE_ID = request.AccountCodeId;
        entity.CHECKFILE = request.CheckFile;
        entity.VAHEDCODE = request.VahedCode;
        entity.ACCOUNTOPENINGDATE = request.AccountOpeningDate;
        entity.CHANGEUSERID = _currentUser.UserId;
        entity.UPDATEDDATE = DateTime.UtcNow;

        await ReconcileTafsiliLinksAsync(request, cancellationToken);

        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    /// <summary>
    /// Brings <c>TB_ACCOUNT_LINK_TAFSILI</c> in line with the request's replacement set, matching
    /// existing rows on the (TAFSILI_ID, LEVEL_ID) pair — the only identity a caller has for a
    /// link (see <see cref="BankAccountTafsiliLinkInput"/>). Only stages work; the caller still
    /// owns the single <see cref="IUnitOfWork.SaveChangesAsync"/>, so the account row and its
    /// links always move together.
    ///
    /// Rows that survive are left completely untouched — not re-stamped with a new
    /// CHANGEUSERID/UPDATEDDATE — so an unrelated edit to the account never rewrites the audit
    /// trail of links the caller did not actually change.
    /// </summary>
    private async Task ReconcileTafsiliLinksAsync(UpdateBankAccountCommand request, CancellationToken cancellationToken)
    {
        var requested = request.TafsiliLinks ?? Array.Empty<BankAccountTafsiliLinkInput>();
        var existing = await _bankAccountRepository.GetActiveTafsiliLinksAsync(request.Id, cancellationToken);

        var requestedKeys = requested
            .Select(l => (l.TafsiliId, l.LevelId))
            .ToHashSet();

        foreach (var link in existing)
        {
            if (requestedKeys.Contains((link.TAFSILI_ID, link.LEVEL_ID)))
            {
                continue;
            }

            link.ISDELETED = true;
            link.CHANGEUSERID = _currentUser.UserId;
            link.UPDATEDDATE = DateTime.UtcNow;
        }

        var existingKeys = existing
            .Select(l => (l.TAFSILI_ID, l.LEVEL_ID))
            .ToHashSet();

        foreach (var link in requested)
        {
            if (existingKeys.Contains((link.TafsiliId, link.LevelId)))
            {
                continue;
            }

            await _bankAccountRepository.AddTafsiliLinkAsync(
                new TB_ACCOUNT_LINK_TAFSILI
                {
                    ID = Guid.NewGuid(),
                    ACCOUNT_ID = request.Id,
                    TAFSILI_ID = link.TafsiliId,
                    LEVEL_ID = link.LevelId,
                    VAHEDCODE = request.VahedCode,
                    ADDUSERID = _currentUser.UserId,
                    CREATEDDATE = DateTime.UtcNow,
                    ISDELETED = false,
                },
                cancellationToken);
        }
    }
}
