using Accounting.Application.Common.Security;
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
///
/// This handler just maps <see cref="UpdateVoucherDetailCommand.VahedCode"/> onto the entity — and,
/// via the post-update <c>entity.VAHEDCODE</c>, onto every تفصیلی link reconciled in the same
/// call — at face value; it does not read <see cref="ICurrentUser.VahedCode"/> directly. Forgery
/// prevention (the record's unit can only ever be set to the caller's own unit, never an
/// arbitrary one) is <c>VahedScopeBehavior</c>'s job — see the scope note on
/// <see cref="UpdateVoucherDetailCommand"/> for what this does and does not close.
/// </summary>
public sealed class UpdateVoucherDetailCommandHandler : IRequestHandler<UpdateVoucherDetailCommand>
{
    private readonly IVoucherDetailRepository _voucherDetailRepository;
    private readonly IVoucherHeadRepository _voucherHeadRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUser _currentUser;
    private readonly IVoucherTafsiliLevelGuard _tafsiliLevelGuard;
    private readonly IVoucherChequeService? _chequeService;
    private readonly IVoucherLineExtrasService? _extrasService;
    private readonly IAccountEntryPolicy? _accountEntryPolicy;

    public UpdateVoucherDetailCommandHandler(
        IVoucherDetailRepository voucherDetailRepository,
        IVoucherHeadRepository voucherHeadRepository,
        IUnitOfWork unitOfWork,
        ICurrentUser currentUser,
        IVoucherTafsiliLevelGuard tafsiliLevelGuard,
        IVoucherChequeService? chequeService = null,
        IVoucherLineExtrasService? extrasService = null,
        IAccountEntryPolicy? accountEntryPolicy = null)
    {
        _chequeService = chequeService;
        _extrasService = extrasService;
        _accountEntryPolicy = accountEntryPolicy;
        _voucherDetailRepository = voucherDetailRepository;
        _voucherHeadRepository = voucherHeadRepository;
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
        _tafsiliLevelGuard = tafsiliLevelGuard;
    }

    public async Task Handle(UpdateVoucherDetailCommand request, CancellationToken cancellationToken)
    {
        var entity = await _voucherDetailRepository.GetForUpdateAsync(request.Id, request.VahedCode, cancellationToken);

        if (entity is null || entity.ISDELETED == true)
        {
            throw new NotFoundException("VoucherDetail", request.Id);
        }

        // Phase 38: a line inherits its parent voucher's editability — a reviewed/accepted
        // voucher is view-only all the way down. The parent is loaded with the caller's
        // VahedCode, so this cannot be used to probe another unit's voucher either.
        if (entity.VOUCHERSHEAD_ID is not null)
        {
            var head = await _voucherHeadRepository.GetForUpdateAsync(
                entity.VOUCHERSHEAD_ID.Value,
                request.VahedCode,
                cancellationToken);

            if (head is not null)
            {
                VoucherEditability.EnsureEditable(head.ID, head.DOCLIFE);

                // ماتریس دسترسی کدینگ (ریسک #۲۷) فقط وقتی معین عوض شود — ویرایش مبلغ/شرح ردیفی که
                // پیش از اعمال ماتریس ثبت شده نباید قفل شود.
                if (_accountEntryPolicy is not null && request.AccountId != entity.ACCOUNT_ID)
                {
                    await _accountEntryPolicy.EnsureManualEntryAllowedAsync(
                        request.VahedCode, head.DATE_DOC, [request.AccountId], cancellationToken);
                }
            }
        }

        var storedLinks = await EnsureTafsiliLevelsSatisfiedAsync(entity, request, cancellationToken);

        var previousCheckId = entity.CHECK_ID;

        entity.ACCOUNT_ID = request.AccountId;
        entity.RECEIP_ID = request.ReceiptId;
        entity.CHECK_ID = request.CheckId;
        entity.LOWLEVELCODE_ID = request.LowLevelCodeId;
        entity.ETEBAR_ID = request.EtebarId;
        entity.DESCRIPTION = request.Description;
        entity.RADIF = request.Radif ?? entity.RADIF;
        entity.DEBTOR = request.Debtor;
        entity.CREDITOR = request.Creditor;
        entity.VAHEDCODE = request.VahedCode;
        entity.YEAR = request.Year;

        var now = DateTime.UtcNow;

        entity.CHANGEUSERID = _currentUser.UserId;
        entity.UPDATEDDATE = now;

        if (request.TafsiliLinks is not null)
        {
            await ReconcileTafsiliLinksAsync(entity, request.TafsiliLinks, storedLinks, now, cancellationToken);
        }

        // دفتر چک — چک ردیف و «در وجه»/تاریخ/شرح آن.
        if (_chequeService is not null)
        {
            entity.CHECK_ID = await _chequeService.ApplyAsync(
                request.CheckId, entity.ID, checkChanged: previousCheckId != request.CheckId, request.Cheque,
                request.VahedCode, cancellationToken);
        }

        // شناسه/ویژگی/فیش — Extras=null یعنی «دست نزن»؛ وگرنه با مقادیر تازه جایگزین می‌شوند.
        if (_extrasService is not null && request.Extras is not null)
        {
            var tafsiliIds = request.TafsiliLinks is not null
                ? request.TafsiliLinks.Select(l => l.TafsiliId).ToList()
                : (await _voucherDetailRepository.GetActiveTafsiliLinksAsync(entity.ID, cancellationToken))
                    .Where(l => l.ISDELETED != true)
                    .Select(l => l.TAFSILI_ID)
                    .ToList();
            await _extrasService.ApplyAsync(entity, tafsiliIds, request.Extras, replace: true, cancellationToken);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    /// <summary>
    /// Applies «تفصیلی الزامی» to the state the line will be left in, not to the request in
    /// isolation — a null <c>TafsiliLinks</c> means "leave the existing assignments alone", so the
    /// stored links are the ones that have to satisfy the rule.
    ///
    /// <b>Deliberately skipped when this request changes neither the تفصیلی nor the حساب.</b>
    /// Rows written before this rule existed can violate it, and there is no way to know how many
    /// (see <c>docs/open-decisions.md</c>). Validating unconditionally would make every such line
    /// permanently uneditable — a user could not fix its شرح or مبلغ — which punishes them for data
    /// they did not create and cannot repair through this endpoint. Validating what the request
    /// actually changes keeps new and edited state correct while leaving untouched history
    /// editable. A request that changes the حساب <i>is</i> re-validated: the stored تفصیلی were
    /// valid for the old حساب and say nothing about the new one.
    /// </summary>
    /// Returns the stored links when it had to read them (so the reconcile step does not read again).
    private async Task<IReadOnlyList<TB_VOUCHERDETAIL_LINK_TAFSILI>?> EnsureTafsiliLevelsSatisfiedAsync(
        TB_VOUCHERSDETAIL entity,
        UpdateVoucherDetailCommand request,
        CancellationToken cancellationToken)
    {
        var accountChanged = entity.ACCOUNT_ID != request.AccountId;

        if (request.TafsiliLinks is null && !accountChanged)
        {
            return null;
        }

        var storedLinks = await _voucherDetailRepository.GetActiveTafsiliLinksAsync(entity.ID, cancellationToken);
        var effectiveLinks = request.TafsiliLinks
            ?? storedLinks
                .Select(link => new VoucherDetailTafsiliLinkInput(link.TAFSILI_ID, link.LEVEL_ID))
                .ToList();

        await _tafsiliLevelGuard.EnsureSatisfiedAsync(request.AccountId, effectiveLinks, cancellationToken);

        // ریسک‌های #۹/#۱۴ — فقط تفصیلی‌های تازه؛ با عوض‌شدن معین همه دوباره کنترل می‌شوند.
        await _tafsiliLevelGuard.EnsureTafsiliSelectableAsync(
            request.AccountId,
            effectiveLinks,
            request.VahedCode,
            accountChanged ? null : storedLinks.Select(link => link.TAFSILI_ID).ToList(),
            cancellationToken);

        return storedLinks;
    }

    /// <summary>
    /// Brings the line's active tafsili links to exactly <paramref name="requestedLinks"/>. Stages
    /// only — the caller still owns the single <see cref="IUnitOfWork.SaveChangesAsync"/>.
    /// </summary>
    private async Task ReconcileTafsiliLinksAsync(
        TB_VOUCHERSDETAIL entity,
        IReadOnlyList<VoucherDetailTafsiliLinkInput> requestedLinks,
        IReadOnlyList<TB_VOUCHERDETAIL_LINK_TAFSILI>? preloadedLinks,
        DateTime now,
        CancellationToken cancellationToken)
    {
        var existingLinks = preloadedLinks ?? await _voucherDetailRepository.GetActiveTafsiliLinksAsync(
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
