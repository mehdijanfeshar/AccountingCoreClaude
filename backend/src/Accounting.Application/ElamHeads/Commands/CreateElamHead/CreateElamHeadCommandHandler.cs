using Accounting.Application.Common.Interfaces;
using Accounting.Domain.Entity;
using MediatR;

namespace Accounting.Application.ElamHeads.Commands.CreateElamHead;

/// <summary>
/// Constructs the <see cref="TB_ELAMHEAD"/> Domain entity from the command, stages it via
/// <see cref="IElamHeadRepository"/>, and owns the transaction boundary by calling
/// <see cref="IUnitOfWork.SaveChangesAsync"/> exactly once. <c>ADDUSERID</c> is sourced from
/// <see cref="ICurrentUser"/> (the authenticated caller) — never from the request — so it
/// cannot be forged by the client, even though the column itself is nullable in Legacy (all
/// four audit columns are nullable on this table, mirroring <c>TB_RABET</c>).
/// <c>request.VahedCode</c> is equally non-forgeable: by the time this handler runs,
/// <c>VahedScopeBehavior</c> has already overwritten it with the authenticated caller's own unit
/// code (see <see cref="CreateElamHeadCommand.VahedCode"/>).
///
/// <c>ID</c> is always generated application-side (<see cref="Guid.NewGuid"/>) — this table has
/// no <c>sys_guid()</c> default in <c>LegacyDbContext</c>, but generating it here regardless
/// keeps this handler symmetric with every other Create handler in the project and never relies
/// on an Oracle DEFAULT.
///
/// ⚠️ HEAD ONLY — no <c>TB_ELAMDETAIL</c> row is ever created here. See
/// <see cref="CreateElamHeadCommand"/> XML doc.
/// </summary>
public sealed class CreateElamHeadCommandHandler : IRequestHandler<CreateElamHeadCommand, Guid>
{
    private readonly IElamHeadRepository _elamHeadRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUser _currentUser;

    public CreateElamHeadCommandHandler(
        IElamHeadRepository elamHeadRepository,
        IUnitOfWork unitOfWork,
        ICurrentUser currentUser)
    {
        _elamHeadRepository = elamHeadRepository;
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
    }

    public async Task<Guid> Handle(CreateElamHeadCommand request, CancellationToken cancellationToken)
    {
        var entity = new TB_ELAMHEAD
        {
            ID = Guid.NewGuid(),
            VOUCHERSHEAD_ID = request.VoucherHeadId,
            ELAMH_SERIALNO = request.SerialNo,
            ELAMH_CODE = request.Code,
            ELAMH_DABIRNO = request.DabirNo,
            ELAMH_DABIRDATE = request.DabirDate,
            ELAMH_PRINTNO = request.PrintNo,
            ELAMH_CASE = request.Case,
            SERIALNO_INPUT = request.SerialNoInput,
            WEB_STAT = request.WebStat,
            ELAMH_DATE = request.Date,
            ELAMH_DESC = request.Desc,
            WORKSHOP_ID = request.WorkShopId,
            ELAMH_RCVNO = request.RcvNo,
            ELAMH_RCVDT = request.RcvDt,
            ELAMH_LSTMON = request.LstMon,
            PAY_NO = request.PayNo,
            ELAMHDRAMAD_TYPE = request.DramadType,
            PEIMAN_NO = request.PeimanNo,
            ELAMH_WORKSHOPCODE = request.WorkShopCode,
            ELAMH_WORKSHOPNAME = request.WorkShopName,
            ELAMH_SENDRCVVAHED = request.SendRcvVahed,
            ELAMH_YEAR = request.ElamYear,
            VAHEDCODE = request.VahedCode,
            YEAR = request.Year,
            ELAMSENDERID = request.ElamSenderId,
            ADDUSERID = _currentUser.UserId,
            CREATEDDATE = DateTime.UtcNow,
            ISDELETED = false,
        };

        await _elamHeadRepository.AddAsync(entity, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return entity.ID;
    }
}
