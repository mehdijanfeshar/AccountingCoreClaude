using Accounting.Application.Common.Interfaces;
using Accounting.Domain.Entity;
using MediatR;

namespace Accounting.Application.Vouchers.Commands.CreateVoucherHead;

/// <summary>
/// Constructs the <see cref="TB_VOUCHERSHEAD"/> Domain entity from the command, stages it via
/// <see cref="IVoucherHeadRepository"/>, and owns the transaction boundary by calling
/// <see cref="IUnitOfWork.SaveChangesAsync"/> exactly once. <c>ADDUSERID</c> is sourced from
/// <see cref="ICurrentUser"/> (the authenticated caller) — never from the request — so it
/// cannot be forged by the client.
/// </summary>
public sealed class CreateVoucherHeadCommandHandler : IRequestHandler<CreateVoucherHeadCommand, Guid>
{
    private readonly IVoucherHeadRepository _voucherHeadRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUser _currentUser;

    public CreateVoucherHeadCommandHandler(
        IVoucherHeadRepository voucherHeadRepository,
        IUnitOfWork unitOfWork,
        ICurrentUser currentUser)
    {
        _voucherHeadRepository = voucherHeadRepository;
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
    }

    public async Task<Guid> Handle(CreateVoucherHeadCommand request, CancellationToken cancellationToken)
    {
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
            CREATEDDATE = DateTime.UtcNow,
            ISDELETED = false,
        };

        await _voucherHeadRepository.AddAsync(entity, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return entity.ID;
    }
}
