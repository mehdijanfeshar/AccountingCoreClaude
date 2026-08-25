using Accounting.Application.Common.Exceptions;
using Accounting.Application.Common.Interfaces;
using Accounting.Application.Vouchers.Commands.Common;
using Accounting.Domain.Entity;
using MediatR;

namespace Accounting.Application.Vouchers.Commands.UpdateVoucherDetail;

/// <summary>
/// Loads the existing <see cref="Accounting.Domain.Entity.TB_VOUCHERSDETAIL"/> row via
/// <see cref="IVoucherDetailRepository.GetForUpdateAsync"/> (change-tracked), overwrites every
/// writable field from the command, stamps audit columns, and owns the transaction boundary
/// by calling <see cref="IUnitOfWork.SaveChangesAsync"/> exactly once. Throws
/// <see cref="NotFoundException"/> — mapped to 404 by <c>GlobalExceptionHandler</c> — when the
/// row does not exist or is already soft-deleted (<c>ISDELETED == true</c>); a soft-deleted row
/// is treated as logically absent for update purposes, consistent with the <c>ISDELETED != true</c>
/// filter used by the read side and with <c>UpdateVoucherHeadCommandHandler</c>'s identical
/// boundary rule. <c>VOUCHERSHEAD_ID</c> is never touched here (see
/// <see cref="UpdateVoucherDetailCommand"/> XML doc for the "no reparenting" rationale).
/// <c>CHANGEUSERID</c> is sourced from <see cref="ICurrentUser"/> (the authenticated caller) —
/// never from the request — so it cannot be forged by the client.
///
/// When <see cref="UpdateVoucherDetailCommand.TafsiliLinks"/> is non-null, this handler also
/// reconciles the line's <see cref="Accounting.Domain.Entity.TB_VOUCHERDETAIL_LINK_TAFSILI"/> rows
/// to exactly the requested set, in the SAME <see cref="IUnitOfWork.SaveChangesAsync"/> as the
/// line's own field updates — so the line and its تفصیلی can never be observed half-updated. The
/// reconcile is a three-way diff keyed on <c>(TAFSILI_ID, LEVEL_ID)</c>:
/// <list type="bullet">
/// <item><description>in DB but not requested → soft-deleted, stamped with the same
/// <c>CHANGEUSERID</c>/<c>UPDATEDDATE</c> as the line itself;</description></item>
/// <item><description>requested but not in DB → inserted via
/// <see cref="IVoucherDetailRepository.AddTafsiliLinkAsync"/>, with
/// <c>VOUCHERSDETAIL_ID</c>/<c>VAHEDCODE</c>/<c>YEAR</c> derived from the line (post-update, so a
/// line that changed unit/year in this same call gets links agreeing with its NEW values);</description></item>
/// <item><description>in both → its <c>VAHEDCODE</c>/<c>YEAR</c> are re-synced to the line's
/// post-update values (a no-op when they didn't change), because those two columns are a derived
/// fact about the parent, not part of the assignment's own history — but its audit columns
/// (<c>ADDUSERID</c>/<c>CREATEDDATE</c>/<c>CHANGEUSERID</c>/<c>UPDATEDDATE</c>) are left
/// byte-identical. Re-stamping audit would destroy the original "who first assigned this تفصیلی"
/// signal on every unrelated line edit; leaving <c>VAHEDCODE</c>/<c>YEAR</c> stale would instead
/// violate the invariant documented on <see cref="VoucherDetailTafsiliLinkInput"/> that a link can
/// never disagree with its own parent about unit/year.</description></item>
/// </list>
/// A <see langword="null"/> list skips the reconcile entirely — no query, no writes — so callers
/// written before this field existed behave byte-identically to before. See the command's XML doc
/// for why null and empty are deliberately not collapsed.
///
/// This does not create independent CRUD for an embedded table: every mutation goes through the
/// parent aggregate's own repository, per the standing team rule.
/// </summary>
public sealed class UpdateVoucherDetailCommandHandler : IRequestHandler<UpdateVoucherDetailCommand>
{
    private readonly IVoucherDetailRepository _voucherDetailRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUser _currentUser;

    public UpdateVoucherDetailCommandHandler(
        IVoucherDetailRepository voucherDetailRepository,
        IUnitOfWork unitOfWork,
        ICurrentUser currentUser)
    {
        _voucherDetailRepository = voucherDetailRepository;
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
    }

    public async Task Handle(UpdateVoucherDetailCommand request, CancellationToken cancellationToken)
    {
        var entity = await _voucherDetailRepository.GetForUpdateAsync(request.Id, cancellationToken);

        if (entity is null || entity.ISDELETED == true)
        {
            throw new NotFoundException("VoucherDetail", request.Id);
        }

        entity.ACCOUNT_ID = request.AccountId;
        entity.RECEIP_ID = request.ReceiptId;
        entity.CHECK_ID = request.CheckId;
        entity.LOWLEVELCODE_ID = request.LowLevelCodeId;
        entity.ETEBAR_ID = request.EtebarId;
        entity.DESCRIPTION = request.Description;
        entity.RADIF = request.Radif;
        entity.DEBTOR = request.Debtor;
        entity.CREDITOR = request.Creditor;
        entity.VAHEDCODE = request.VahedCode;
        entity.YEAR = request.Year;

        var now = DateTime.UtcNow;

        entity.CHANGEUSERID = _currentUser.UserId;
        entity.UPDATEDDATE = now;

        if (request.TafsiliLinks is not null)
        {
            await ReconcileTafsiliLinksAsync(entity, request.TafsiliLinks, now, cancellationToken);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    /// <summary>
    /// Brings the line's active tafsili links to exactly <paramref name="requestedLinks"/>. Stages
    /// only — the caller still owns the single <see cref="IUnitOfWork.SaveChangesAsync"/>.
    /// </summary>
    private async Task ReconcileTafsiliLinksAsync(
        TB_VOUCHERSDETAIL entity,
        IReadOnlyList<VoucherDetailTafsiliLinkInput> requestedLinks,
        DateTime now,
        CancellationToken cancellationToken)
    {
        var existingLinks = await _voucherDetailRepository.GetActiveTafsiliLinksAsync(
            entity.ID, cancellationToken);

        // Duplicate (TafsiliId, LevelId) pairs in the request collapse to one — the same تفصیلی
        // assigned twice to one line is one assignment, and the Legacy table has no UNIQUE
        // constraint that would reject the second row.
        var requestedKeys = requestedLinks
            .Select(link => (link.TafsiliId, link.LevelId))
            .ToHashSet();

        // Dropped by the caller → soft-delete, sharing the line's own audit stamp.
        foreach (var existingLink in existingLinks)
        {
            if (requestedKeys.Contains((existingLink.TAFSILI_ID, existingLink.LEVEL_ID)))
            {
                // Kept — sync the derived VAHEDCODE/YEAR to the line's new values (no-op if
                // unchanged), but never touch audit columns for a link that wasn't actually
                // dropped or newly assigned.
                existingLink.VAHEDCODE = entity.VAHEDCODE;
                existingLink.YEAR = entity.YEAR;
                continue;
            }

            existingLink.ISDELETED = true;
            existingLink.CHANGEUSERID = _currentUser.UserId;
            existingLink.UPDATEDDATE = now;
        }

        // Seeded with what is already active, so a pair present in both sets is left untouched
        // rather than duplicated. Adding to this set as we go also collapses request duplicates.
        var keysAlreadyPresent = existingLinks
            .Select(link => (link.TAFSILI_ID, link.LEVEL_ID))
            .ToHashSet();

        foreach (var requestedLink in requestedLinks)
        {
            if (!keysAlreadyPresent.Add((requestedLink.TafsiliId, requestedLink.LevelId)))
            {
                continue;
            }

            var linkEntity = new TB_VOUCHERDETAIL_LINK_TAFSILI
            {
                ID = Guid.NewGuid(),
                VOUCHERSDETAIL_ID = entity.ID,
                TAFSILI_ID = requestedLink.TafsiliId,
                LEVEL_ID = requestedLink.LevelId,
                // Read from the entity AFTER its own fields were overwritten above, so a line that
                // changed unit/year in this same call gets links agreeing with its new values.
                VAHEDCODE = entity.VAHEDCODE,
                YEAR = entity.YEAR,
                ADDUSERID = _currentUser.UserId,
                CREATEDDATE = now,
                ISDELETED = false,
            };

            await _voucherDetailRepository.AddTafsiliLinkAsync(linkEntity, cancellationToken);
        }
    }
}
