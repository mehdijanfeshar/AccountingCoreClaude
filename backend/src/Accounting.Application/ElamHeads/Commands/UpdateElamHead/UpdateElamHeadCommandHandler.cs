using Accounting.Application.Common.Exceptions;
using Accounting.Application.Common.Interfaces;
using MediatR;

namespace Accounting.Application.ElamHeads.Commands.UpdateElamHead;

/// <summary>
/// Loads the existing <see cref="Accounting.Domain.Entity.TB_ELAMHEAD"/> row via
/// <see cref="IElamHeadRepository.GetForUpdateAsync"/> (change-tracked), overwrites every
/// writable field from the command, stamps audit columns, and owns the transaction boundary by
/// calling <see cref="IUnitOfWork.SaveChangesAsync"/> exactly once. Throws
/// <see cref="NotFoundException"/> — mapped to 404 by <c>GlobalExceptionHandler</c> — when the
/// row does not exist or is already soft-deleted. <c>ISDELETED</c> is <c>bool?</c> on this
/// table, so both <see langword="false"/> and <see langword="null"/> are treated as
/// "not deleted" — only an explicit <see langword="true"/> triggers 404, consistent with the
/// <c>ISDELETED != true</c> filter used by the read side. <c>CHANGEUSERID</c> is sourced from
/// <see cref="ICurrentUser"/> (the authenticated caller) — never from the request.
/// <c>request.VahedCode</c> is equally non-forgeable: by the time this handler runs,
/// <c>VahedScopeBehavior</c> has already overwritten it with the authenticated caller's own unit
/// code (see <see cref="UpdateElamHeadCommand.VahedCode"/>).
///
/// ⚠️ HEAD ONLY — no <c>TB_ELAMDETAIL</c> row is ever touched here. See
/// <see cref="UpdateElamHeadCommand"/> XML doc.
/// </summary>
public sealed class UpdateElamHeadCommandHandler : IRequestHandler<UpdateElamHeadCommand>
{
    private readonly IElamHeadRepository _elamHeadRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUser _currentUser;

    public UpdateElamHeadCommandHandler(
        IElamHeadRepository elamHeadRepository,
        IUnitOfWork unitOfWork,
        ICurrentUser currentUser)
    {
        _elamHeadRepository = elamHeadRepository;
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
    }

    public async Task Handle(UpdateElamHeadCommand request, CancellationToken cancellationToken)
    {
        var entity = await _elamHeadRepository.GetForUpdateAsync(request.Id, cancellationToken);

        if (entity is null || entity.ISDELETED == true)
        {
            throw new NotFoundException("ElamHead", request.Id);
        }

        entity.VOUCHERSHEAD_ID = request.VoucherHeadId;
        entity.ELAMH_SERIALNO = request.SerialNo;
        entity.ELAMH_CODE = request.Code;
        entity.ELAMH_DABIRNO = request.DabirNo;
        entity.ELAMH_DABIRDATE = request.DabirDate;
        entity.ELAMH_PRINTNO = request.PrintNo;
        entity.ELAMH_CASE = request.Case;
        entity.SERIALNO_INPUT = request.SerialNoInput;
        entity.WEB_STAT = request.WebStat;
        entity.ELAMH_DATE = request.Date;
        entity.ELAMH_DESC = request.Desc;
        entity.WORKSHOP_ID = request.WorkShopId;
        entity.ELAMH_RCVNO = request.RcvNo;
        entity.ELAMH_RCVDT = request.RcvDt;
        entity.ELAMH_LSTMON = request.LstMon;
        entity.PAY_NO = request.PayNo;
        entity.ELAMHDRAMAD_TYPE = request.DramadType;
        entity.PEIMAN_NO = request.PeimanNo;
        entity.ELAMH_WORKSHOPCODE = request.WorkShopCode;
        entity.ELAMH_WORKSHOPNAME = request.WorkShopName;
        entity.ELAMH_SENDRCVVAHED = request.SendRcvVahed;
        entity.ELAMH_YEAR = request.ElamYear;
        entity.VAHEDCODE = request.VahedCode;
        entity.YEAR = request.Year;
        entity.ELAMSENDERID = request.ElamSenderId;
        entity.CHANGEUSERID = _currentUser.UserId;
        entity.UPDATEDDATE = DateTime.UtcNow;

        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
