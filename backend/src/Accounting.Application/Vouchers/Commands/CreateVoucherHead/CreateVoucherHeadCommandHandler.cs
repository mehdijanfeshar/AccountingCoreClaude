using Accounting.Application.Common.Interfaces;
using Accounting.Domain.Entity;
using MediatR;

namespace Accounting.Application.Vouchers.Commands.CreateVoucherHead;

/// <summary>
/// Constructs the <see cref="TB_VOUCHERSHEAD"/> Domain entity from the command, stages it via
/// <see cref="IVoucherHeadRepository"/>, and — when <see cref="CreateVoucherHeadCommand.InitialDetails"/>
/// is supplied — also constructs and stages one <see cref="TB_VOUCHERSDETAIL"/> per entry via
/// <see cref="IVoucherDetailRepository"/>, wiring each line's <c>VOUCHERSHEAD_ID</c>,
/// <c>VAHEDCODE</c> and <c>YEAR</c> to the head just built (never taken from the line input —
/// see <see cref="CreateVoucherHeadDetailInput"/> XML doc). Owns the transaction boundary by
/// calling <see cref="IUnitOfWork.SaveChangesAsync"/> exactly ONCE for the head AND every
/// initial detail line together, so a partially-created voucher (head without its opening
/// lines, or vice versa) can never be observed. <c>ADDUSERID</c> — on the head and on every
/// detail line — is sourced from <see cref="ICurrentUser"/> (the authenticated caller) — never
/// from the request — so it cannot be forged by the client. <c>CREATEDDATE</c> is computed once
/// (<c>now</c>) and reused for the head and every detail line, so they share one creation
/// timestamp instead of drifting by however long line construction takes.
/// </summary>
public sealed class CreateVoucherHeadCommandHandler : IRequestHandler<CreateVoucherHeadCommand, Guid>
{
    private readonly IVoucherHeadRepository _voucherHeadRepository;
    private readonly IVoucherDetailRepository _voucherDetailRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUser _currentUser;

    public CreateVoucherHeadCommandHandler(
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

    public async Task<Guid> Handle(CreateVoucherHeadCommand request, CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;

        var entity = new TB_VOUCHERSHEAD
        {
            ID = Guid.NewGuid(),
            DOC_NUM = request.DocNum,
            DATE_DOC = request.DateDoc,
            DOCLIFE = request.DocLife,
            HEAD_DESC = request.HeadDesc,
            APENDIX = request.Apendix,
            SYSTEM_TYPE = request.SystemTypeId,
            FLAG_STATE = request.FlagState,
            VAHEDCODE = request.VahedCode,
            YEAR = request.Year,
            ISAUTOMATIC = request.IsAutomatic,
            SNDVAHEDCODE = request.SndVahedCode,
            PARENTHEAD_ID = request.ParentHeadId,
            ATTACHFILE_NAME = request.AttachFileName,
            ATF_NUM = request.AtfNum,
            ADDUSERID = _currentUser.UserId,
            CREATEDDATE = now,
            ISDELETED = false,
        };

        await _voucherHeadRepository.AddAsync(entity, cancellationToken);

        if (request.InitialDetails is { Count: > 0 } initialDetails)
        {
            foreach (var detailInput in initialDetails)
            {
                var detailEntity = new TB_VOUCHERSDETAIL
                {
                    ID = Guid.NewGuid(),
                    VOUCHERSHEAD_ID = entity.ID,
                    ACCOUNT_ID = detailInput.AccountId,
                    RECEIP_ID = detailInput.ReceiptId,
                    CHECK_ID = detailInput.CheckId,
                    LOWLEVELCODE_ID = detailInput.LowLevelCodeId,
                    ETEBAR_ID = detailInput.EtebarId,
                    DESCRIPTION = detailInput.Description,
                    RADIF = detailInput.Radif,
                    DEBTOR = detailInput.Debtor,
                    CREDITOR = detailInput.Creditor,
                    VAHEDCODE = request.VahedCode,
                    YEAR = request.Year,
                    ADDUSERID = _currentUser.UserId,
                    CREATEDDATE = now,
                    ISDELETED = false,
                };

                await _voucherDetailRepository.AddAsync(detailEntity, cancellationToken);
            }
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return entity.ID;
    }
}
