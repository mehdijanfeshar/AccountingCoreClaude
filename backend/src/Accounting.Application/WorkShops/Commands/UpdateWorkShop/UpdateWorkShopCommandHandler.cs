using Accounting.Application.Common.Exceptions;
using Accounting.Application.WorkShops.Commands.Common;
using Accounting.Application.Common.Interfaces;
using Accounting.Domain.Entity;
using MediatR;

namespace Accounting.Application.WorkShops.Commands.UpdateWorkShop;

/// <summary>
/// Loads the existing <see cref="Accounting.Domain.Entity.TB_WORKSHOP"/> row via
/// <see cref="IWorkShopRepository.GetForUpdateAsync"/> (change-tracked), overwrites every
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
/// <c>TB_WORKSHOP.VAHEDCODE</c> at face value — see <c>UpdateWorkShopCommand</c> XML doc for the
/// explicit scope note on what this does and does not cover.
/// </summary>
public sealed class UpdateWorkShopCommandHandler : IRequestHandler<UpdateWorkShopCommand>
{
    private readonly IWorkShopRepository _workShopRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUser _currentUser;

    public UpdateWorkShopCommandHandler(
        IWorkShopRepository workShopRepository,
        IUnitOfWork unitOfWork,
        ICurrentUser currentUser)
    {
        _workShopRepository = workShopRepository;
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
    }

    public async Task Handle(UpdateWorkShopCommand request, CancellationToken cancellationToken)
    {
        var entity = await _workShopRepository.GetForUpdateAsync(request.Id, request.VahedCode, cancellationToken);

        if (entity is null || entity.ISDELETED == true)
        {
            throw new NotFoundException("WorkShop", request.Id);
        }

        entity.ACCOUNTCODE_ID = request.AccountCodeId;
        entity.BRANCH_ID = request.BranchId;
        entity.WORKSHOPNAME = request.WorkShopName;
        entity.WORKSHOPCODE = request.WorkShopCode;
        entity.VAHEDCODE = request.VahedCode;
        entity.ISACTIVE = request.IsActive;
        entity.CHECKFILE = request.CheckFile;
        entity.CHANGEUSERID = _currentUser.UserId;
        entity.UPDATEDDATE = DateTime.UtcNow;

        await ReconcileTafsiliLinksAsync(request, cancellationToken);

        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    /// <summary>
    /// Brings <c>TB_WORKSHOP_LINK_TAFSILI</c> in line with the request's replacement set, matching existing
    /// rows on the (TAFSILI_ID, LEVEL_ID) pair — the only identity a caller has for a link. Only
    /// stages work; the caller still owns the single <see cref="IUnitOfWork.SaveChangesAsync"/>.
    ///
    /// Surviving rows are left completely untouched — not re-stamped — so an unrelated edit never
    /// rewrites the audit trail of links the caller did not actually change. Mirrors
    /// <c>UpdateBankAccountCommandHandler.ReconcileTafsiliLinksAsync</c>.
    /// </summary>
    private async Task ReconcileTafsiliLinksAsync(UpdateWorkShopCommand request, CancellationToken cancellationToken)
    {
        var requested = request.TafsiliLinks ?? Array.Empty<WorkShopTafsiliLinkInput>();
        var existing = await _workShopRepository.GetActiveTafsiliLinksAsync(request.Id, cancellationToken);

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

            await _workShopRepository.AddTafsiliLinkAsync(
                new TB_WORKSHOP_LINK_TAFSILI
                {
                    ID = Guid.NewGuid(),
                    WORKSHOP_ID = request.Id,
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
