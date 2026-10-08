using Accounting.Application.Common.Security;
using Accounting.Application.Common.Exceptions;
using Accounting.Application.Common.Interfaces;
using Accounting.Application.Vouchers.Commands.Common;
using MediatR;

namespace Accounting.Application.Vouchers.Commands.UpdateVoucherHead;

/// <summary>
/// Loads the existing <see cref="Accounting.Domain.Entity.TB_VOUCHERSHEAD"/> row via
/// <see cref="IVoucherHeadRepository.GetForUpdateAsync"/> (change-tracked), overwrites every
/// writable field from the command, stamps audit columns, and owns the transaction boundary
/// by calling <see cref="IUnitOfWork.SaveChangesAsync"/> exactly once. Throws
/// <see cref="NotFoundException"/> — mapped to 404 by <c>GlobalExceptionHandler</c> — when the
/// row does not exist or is already soft-deleted (<c>ISDELETED == true</c>); a soft-deleted
/// row is treated as logically absent for update purposes, consistent with the
/// <c>ISDELETED != true</c> filter used by the read side. <c>GLOBALNUMBER</c> and
/// <c>ATTACHFILE</c> are never touched here (see <see cref="UpdateVoucherHeadCommand"/> doc).
/// <c>CHANGEUSERID</c> is sourced from <see cref="ICurrentUser"/> (the authenticated caller)
/// — never from the request — so it cannot be forged by the client, mirroring the audit
/// pattern already used by <c>CreateVoucherHeadCommandHandler</c>.
///
/// This handler just maps <see cref="UpdateVoucherHeadCommand.VahedCode"/> onto the entity at
/// face value; it does not read <see cref="ICurrentUser.VahedCode"/> directly. Forgery
/// prevention (the record's unit can only ever be set to the caller's own unit, never an
/// arbitrary one) is <c>VahedScopeBehavior</c>'s job — see the scope note on
/// <see cref="UpdateVoucherHeadCommand"/> for what this does and does not close.
/// </summary>
public sealed class UpdateVoucherHeadCommandHandler : IRequestHandler<UpdateVoucherHeadCommand>
{
    private readonly IVoucherHeadRepository _voucherHeadRepository;
    private readonly IVoucherDetailRepository _voucherDetailRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUser _currentUser;

    public UpdateVoucherHeadCommandHandler(
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

    public async Task Handle(UpdateVoucherHeadCommand request, CancellationToken cancellationToken)
    {
        var entity = await _voucherHeadRepository.GetForUpdateAsync(request.Id, request.VahedCode, cancellationToken);

        if (entity is null || entity.ISDELETED == true)
        {
            throw new NotFoundException("VoucherHead", request.Id);
        }

        // Phase 38: a reviewed/accepted voucher is view-only. Team working-rule #1 — this rule
        // must not live only in the cartable UI.
        VoucherEditability.EnsureEditable(entity.ID, entity.DOCLIFE);

        // Risk #5 (owner 2026-10-07): only یادداشت ↔ موقت here (validator); null keeps the state.
        // Moving یادداشت → موقت needs a balanced voucher (risk #3).
        var newState = request.DocLife ?? entity.DOCLIFE;
        if (newState != entity.DOCLIFE && VoucherBalanceGuard.RequiresBalance(newState))
        {
            await VoucherBalanceGuard.EnsureBalancedAsync(_voucherDetailRepository, [entity], cancellationToken);
        }

        entity.DOC_NUM = request.DocNum;
        entity.DATE_DOC = request.DateDoc;
        entity.DOCLIFE = newState;
        entity.HEAD_DESC = request.HeadDesc;
        entity.APENDIX = request.Apendix;
        entity.SYSTEM_TYPE = request.SystemTypeId;
        entity.FLAG_STATE = request.FlagState;
        entity.VAHEDCODE = request.VahedCode;
        entity.YEAR = request.Year;
        entity.ISAUTOMATIC = request.IsAutomatic;
        entity.SNDVAHEDCODE = request.SndVahedCode;
        entity.PARENTHEAD_ID = request.ParentHeadId;
        entity.ATTACHFILE_NAME = request.AttachFileName;
        // ATF_NUM عمداً دست نمی‌خورد: شمارهٔ عطف هنگام ایجاد تخصیص می‌یابد و هرگز عوض نمی‌شود.
        entity.CHANGEUSERID = _currentUser.UserId;
        entity.UPDATEDDATE = DateTime.UtcNow;

        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
