using Accounting.Application.Common.Interfaces;
using Accounting.Domain.Entity;
using MediatR;

namespace Accounting.Application.ChequesIncorrents.Commands.CreateChequesIncorrent;

/// <summary>
/// Constructs the <see cref="TB_CHEQUES_INCORRENT"/> Domain entity from the command, stages it
/// via <see cref="IChequesIncorrentRepository"/>, and owns the transaction boundary by calling
/// <see cref="IUnitOfWork.SaveChangesAsync"/> exactly once. <c>ADDUSERID</c> is sourced from
/// <see cref="ICurrentUser"/> (the authenticated caller) — never from the request — so it cannot
/// be forged by the client. <c>request.VahedCode</c> is equally non-forgeable: by the time this
/// handler runs, <c>VahedScopeBehavior</c> has already overwritten it with the authenticated
/// caller's own unit code (see <see cref="CreateChequesIncorrentCommand.VahedCode"/>).
///
/// <c>ID</c> is always generated application-side (<see cref="Guid.NewGuid"/>); the Oracle
/// column has no <c>sys_guid()</c> default, so this simply follows the project-wide convention.
/// </summary>
public sealed class CreateChequesIncorrentCommandHandler : IRequestHandler<CreateChequesIncorrentCommand, Guid>
{
    private readonly IChequesIncorrentRepository _chequesIncorrentRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUser _currentUser;

    public CreateChequesIncorrentCommandHandler(
        IChequesIncorrentRepository chequesIncorrentRepository,
        IUnitOfWork unitOfWork,
        ICurrentUser currentUser)
    {
        _chequesIncorrentRepository = chequesIncorrentRepository;
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
    }

    public async Task<Guid> Handle(CreateChequesIncorrentCommand request, CancellationToken cancellationToken)
    {
        var entity = new TB_CHEQUES_INCORRENT
        {
            ID = Guid.NewGuid(),
            CHECK_ID = request.CheckId,
            DOC_NUM = request.DocNum,
            DOC_DATE = request.DocDate,
            CHEQ_NO = request.CheqNo,
            CHEQ_DATE = request.CheqDate,
            PAPER_DESC = request.PaperDesc,
            PAYTO = request.PayTo,
            RECIVDATE = request.RecivDate,
            ACCOUNTNUMBER = request.AccountNumber,
            CREDITOR = request.Creditor,
            VAHEDCODE = request.VahedCode,
            YEAR = request.Year,
            ADDUSERID = _currentUser.UserId,
            CREATEDDATE = DateTime.UtcNow,
            ISDELETED = false,
        };

        await _chequesIncorrentRepository.AddAsync(entity, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return entity.ID;
    }
}
