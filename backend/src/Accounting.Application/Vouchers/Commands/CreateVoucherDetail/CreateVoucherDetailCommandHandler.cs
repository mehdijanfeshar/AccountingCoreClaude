using Accounting.Application.Common.Exceptions;
using Accounting.Application.Common.Interfaces;
using Accounting.Domain.Entity;
using MediatR;

namespace Accounting.Application.Vouchers.Commands.CreateVoucherDetail;

/// <summary>
/// Pre-checks the parent <see cref="TB_VOUCHERSHEAD"/> via
/// <see cref="IVoucherHeadRepository.GetForUpdateAsync"/> — throwing <see cref="NotFoundException"/>
/// (mapped to 404 by <c>GlobalExceptionHandler</c>) when it does not exist or is already
/// soft-deleted (<c>ISDELETED == true</c>) — then constructs the <see cref="TB_VOUCHERSDETAIL"/>
/// Domain entity from the command, stages it via <see cref="IVoucherDetailRepository"/>, and
/// owns the transaction boundary by calling <see cref="IUnitOfWork.SaveChangesAsync"/> exactly
/// once. <c>ADDUSERID</c> is sourced from <see cref="ICurrentUser"/> (the authenticated caller)
/// — never from the request — so it cannot be forged by the client.
///
/// The parent-head check is NOT an invented business rule — the entire premise of "add a line
/// to an existing voucher" already presupposes the voucher exists, so verifying it is the
/// operation's own semantics. Without it, the caller would instead get a raw, undocumented
/// <c>DbUpdateException</c>/ORA-02291 (FK violation on <c>FK_VOUCHERHEAD</c>) surfacing as an
/// unhelpful 500.
///
/// When <see cref="CreateVoucherDetailCommand.TafsiliLinks"/> is supplied, one
/// <see cref="TB_VOUCHERDETAIL_LINK_TAFSILI"/> row per distinct <c>(TafsiliId, LevelId)</c> pair is
/// staged via <see cref="IVoucherDetailRepository.AddTafsiliLinkAsync"/> BEFORE that same single
/// <see cref="IUnitOfWork.SaveChangesAsync"/>, so the line and its تفصیلی are persisted atomically —
/// a line can never be observed without the تفصیلی it was created with. Each link's
/// <c>VOUCHERSDETAIL_ID</c>/<c>VAHEDCODE</c>/<c>YEAR</c> are wired from the line just built, never
/// taken from the link input (see <see cref="Accounting.Application.Vouchers.Commands.Common.VoucherDetailTafsiliLinkInput"/>),
/// and every link shares the line's single <c>CREATEDDATE</c> value rather than re-reading the
/// clock per row — the same rule <c>CreateVoucherHeadCommandHandler</c> applies to a head and its
/// initial lines.
///
/// <b>Known, documented gap (open item):</b> this is a check-then-act pattern, not a
/// transactional guarantee — a race where the head is soft-deleted by a concurrent request
/// between this check and the later <see cref="IUnitOfWork.SaveChangesAsync"/> call still falls
/// through to the same DB FK and would surface as a 500 in that narrow window. No idempotency
/// key or pessimistic lock was introduced to close this window; it remains open.
/// </summary>
public sealed class CreateVoucherDetailCommandHandler : IRequestHandler<CreateVoucherDetailCommand, Guid>
{
    private readonly IVoucherHeadRepository _voucherHeadRepository;
    private readonly IVoucherDetailRepository _voucherDetailRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUser _currentUser;

    public CreateVoucherDetailCommandHandler(
        IVoucherHeadRepository voucherHeadRepository,
        IVoucherDetailRepository voucherDetailRepository,
        IUnitOfWork unitOfWork,
        ICurrentUser currentUser)
    {
        _voucherHeadRepository = voucherHeadRepository;
        _voucherDetailRepository = voucherDetailRepository;
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
    }

    public async Task<Guid> Handle(CreateVoucherDetailCommand request, CancellationToken cancellationToken)
    {
        var head = await _voucherHeadRepository.GetForUpdateAsync(request.VoucherHeadId, cancellationToken);

        if (head is null || head.ISDELETED == true)
        {
            throw new NotFoundException("VoucherHead", request.VoucherHeadId);
        }

        var now = DateTime.UtcNow;

        var entity = new TB_VOUCHERSDETAIL
        {
            ID = Guid.NewGuid(),
            VOUCHERSHEAD_ID = request.VoucherHeadId,
            ACCOUNT_ID = request.AccountId,
            RECEIP_ID = request.ReceiptId,
            CHECK_ID = request.CheckId,
            LOWLEVELCODE_ID = request.LowLevelCodeId,
            ETEBAR_ID = request.EtebarId,
            DESCRIPTION = request.Description,
            RADIF = request.Radif,
            DEBTOR = request.Debtor,
            CREDITOR = request.Creditor,
            VAHEDCODE = request.VahedCode,
            YEAR = request.Year,
            ADDUSERID = _currentUser.UserId,
            CREATEDDATE = now,
            ISDELETED = false,
        };

        await _voucherDetailRepository.AddAsync(entity, cancellationToken);

        if (request.TafsiliLinks is { Count: > 0 } tafsiliLinks)
        {
            // Collapse duplicate (TafsiliId, LevelId) pairs: the same تفصیلی assigned twice to one
            // line is one assignment, and TB_VOUCHERDETAIL_LINK_TAFSILI has no UNIQUE constraint
            // to reject the second row, so writing both would leave duplicate data behind.
            var stagedKeys = new HashSet<(Guid TafsiliId, Guid LevelId)>();

            foreach (var linkInput in tafsiliLinks)
            {
                if (!stagedKeys.Add((linkInput.TafsiliId, linkInput.LevelId)))
                {
                    continue;
                }

                var linkEntity = new TB_VOUCHERDETAIL_LINK_TAFSILI
                {
                    ID = Guid.NewGuid(),
                    VOUCHERSDETAIL_ID = entity.ID,
                    TAFSILI_ID = linkInput.TafsiliId,
                    LEVEL_ID = linkInput.LevelId,
                    VAHEDCODE = entity.VAHEDCODE,
                    YEAR = entity.YEAR,
                    ADDUSERID = _currentUser.UserId,
                    CREATEDDATE = now,
                    ISDELETED = false,
                };

                await _voucherDetailRepository.AddTafsiliLinkAsync(linkEntity, cancellationToken);
            }
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return entity.ID;
    }
}
