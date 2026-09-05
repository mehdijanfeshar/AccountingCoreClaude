using Accounting.Application.Common.Exceptions;
using Accounting.Application.Common.Interfaces;
using MediatR;

namespace Accounting.Application.ChequesIncorrents.Commands.UpdateChequesIncorrent;

/// <summary>
/// Loads the existing <see cref="Accounting.Domain.Entity.TB_CHEQUES_INCORRENT"/> row via
/// <see cref="IChequesIncorrentRepository.GetForUpdateAsync"/> (change-tracked), overwrites
/// every writable field from the command, stamps audit columns, and owns the transaction
/// boundary by calling <see cref="IUnitOfWork.SaveChangesAsync"/> exactly once. Throws
/// <see cref="NotFoundException"/> — mapped to 404 by <c>GlobalExceptionHandler</c> — when the
/// row does not exist or is already soft-deleted (<c>ISDELETED</c> is non-nullable <c>bool</c>
/// on this table). <c>CHANGEUSERID</c> is sourced from <see cref="ICurrentUser"/> — never from
/// the request.
/// </summary>
public sealed class UpdateChequesIncorrentCommandHandler : IRequestHandler<UpdateChequesIncorrentCommand>
{
    private readonly IChequesIncorrentRepository _chequesIncorrentRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUser _currentUser;

    public UpdateChequesIncorrentCommandHandler(
        IChequesIncorrentRepository chequesIncorrentRepository,
        IUnitOfWork unitOfWork,
        ICurrentUser currentUser)
    {
        _chequesIncorrentRepository = chequesIncorrentRepository;
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
    }

    public async Task Handle(UpdateChequesIncorrentCommand request, CancellationToken cancellationToken)
    {
        var entity = await _chequesIncorrentRepository.GetForUpdateAsync(request.Id, cancellationToken);

        if (entity is null || entity.ISDELETED)
        {
            throw new NotFoundException("ChequesIncorrent", request.Id);
        }

        entity.CHECK_ID = request.CheckId;
        entity.DOC_NUM = request.DocNum;
        entity.DOC_DATE = request.DocDate;
        entity.CHEQ_NO = request.CheqNo;
        entity.CHEQ_DATE = request.CheqDate;
        entity.PAPER_DESC = request.PaperDesc;
        entity.PAYTO = request.PayTo;
        entity.RECIVDATE = request.RecivDate;
        entity.ACCOUNTNUMBER = request.AccountNumber;
        entity.CREDITOR = request.Creditor;
        entity.VAHEDCODE = request.VahedCode;
        entity.YEAR = request.Year;
        entity.CHANGEUSERID = _currentUser.UserId;
        entity.UPDATEDDATE = DateTime.UtcNow;

        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
