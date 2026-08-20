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
            CREATEDDATE = DateTime.UtcNow,
            ISDELETED = false,
        };

        await _voucherDetailRepository.AddAsync(entity, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return entity.ID;
    }
}
