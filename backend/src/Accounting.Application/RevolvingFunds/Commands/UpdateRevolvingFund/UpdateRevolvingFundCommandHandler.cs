using Accounting.Application.Common.Exceptions;
using Accounting.Application.RevolvingFunds.Commands.Common;
using Accounting.Application.Common.Interfaces;
using Accounting.Domain.Entity;
using MediatR;

namespace Accounting.Application.RevolvingFunds.Commands.UpdateRevolvingFund;

/// <summary>
/// Loads the existing <see cref="Accounting.Domain.Entity.TB_REVOLVING_FUND"/> row via
/// <see cref="IRevolvingFundRepository.GetForUpdateAsync"/> (change-tracked), overwrites every
/// writable field from the command, stamps audit columns, and owns the transaction boundary by
/// calling <see cref="IUnitOfWork.SaveChangesAsync"/> exactly once. Throws
/// <see cref="NotFoundException"/> — mapped to 404 by <c>GlobalExceptionHandler</c> — when the
/// row does not exist or is already soft-deleted. <c>ISDELETED</c> is <c>bool?</c> on this
/// table, so both <see langword="false"/> and <see langword="null"/> are treated as
/// "not deleted" — only an explicit <see langword="true"/> triggers 404, consistent with the
/// <c>ISDELETED != true</c> filter used by the read side. <c>CHANGEUSERID</c> is sourced from
/// <see cref="ICurrentUser"/> (the authenticated caller) — never from the request.
///
/// <c>request.VahedCode</c> is likewise unforgeable, enforced one layer earlier: by the time
/// this handler runs, <c>VahedScopeBehavior</c> has already overwritten it with
/// <see cref="ICurrentUser.VahedCode"/>, so this handler simply maps it onto
/// <c>TB_REVOLVING_FUND.VAHEDCODE</c> at face value — see <c>UpdateRevolvingFundCommand</c> XML
/// doc for the explicit scope note on what this does and does not cover.
/// </summary>
public sealed class UpdateRevolvingFundCommandHandler : IRequestHandler<UpdateRevolvingFundCommand>
{
    private readonly IRevolvingFundRepository _revolvingFundRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUser _currentUser;

    public UpdateRevolvingFundCommandHandler(
        IRevolvingFundRepository revolvingFundRepository,
        IUnitOfWork unitOfWork,
        ICurrentUser currentUser)
    {
        _revolvingFundRepository = revolvingFundRepository;
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
    }

    public async Task Handle(UpdateRevolvingFundCommand request, CancellationToken cancellationToken)
    {
        var entity = await _revolvingFundRepository.GetForUpdateAsync(request.Id, cancellationToken);

        if (entity is null || entity.ISDELETED == true)
        {
            throw new NotFoundException("RevolvingFund", request.Id);
        }

        entity.CODE = request.Code;
        entity.NAME = request.Name;
        entity.DESCRIPTION = request.Description;
        entity.DEFAULTAMOUNT = request.DefaultAmount;
        entity.ACCOUNTCODE_ID = request.AccountCodeId;
        entity.VAHEDCODE = request.VahedCode;
        entity.YEAR = request.Year;
        entity.CHANGEUSERID = _currentUser.UserId;
        entity.UPDATEDDATE = DateTime.UtcNow;

        await ReconcileTafsiliLinksAsync(request, cancellationToken);

        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    /// <summary>
    /// Brings <c>TB_REVOLVINGFUND_LINK_TAFSILI</c> in line with the request's replacement set, matching existing
    /// rows on the (TAFSILI_ID, LEVEL_ID) pair — the only identity a caller has for a link. Only
    /// stages work; the caller still owns the single <see cref="IUnitOfWork.SaveChangesAsync"/>.
    ///
    /// Surviving rows are left completely untouched — not re-stamped — so an unrelated edit never
    /// rewrites the audit trail of links the caller did not actually change. Mirrors
    /// <c>UpdateBankAccountCommandHandler.ReconcileTafsiliLinksAsync</c>.
    /// </summary>
    private async Task ReconcileTafsiliLinksAsync(UpdateRevolvingFundCommand request, CancellationToken cancellationToken)
    {
        var requested = request.TafsiliLinks ?? Array.Empty<RevolvingFundTafsiliLinkInput>();
        var existing = await _revolvingFundRepository.GetActiveTafsiliLinksAsync(request.Id, cancellationToken);

        var requestedKeys = requested.Select(l => (l.TafsiliId, l.LevelId)).ToHashSet();

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

        var existingKeys = existing.Select(l => (l.TAFSILI_ID, l.LEVEL_ID)).ToHashSet();

        foreach (var link in requested)
        {
            if (existingKeys.Contains((link.TafsiliId, link.LevelId)))
            {
                continue;
            }

            await _revolvingFundRepository.AddTafsiliLinkAsync(
                new TB_REVOLVINGFUND_LINK_TAFSILI
                {
                    ID = Guid.NewGuid(),
                    REVOLVINGFUND_ID = request.Id,
                    TAFSILI_ID = link.TafsiliId,
                    LEVEL_ID = link.LevelId,
                    VAHEDCODE = request.VahedCode,
                    YEAR = request.Year,
                    ADDUSERID = _currentUser.UserId,
                    CREATEDDATE = DateTime.UtcNow,
                    ISDELETED = false,
                },
                cancellationToken);
        }
    }
}
