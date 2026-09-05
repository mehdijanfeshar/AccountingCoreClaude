using Accounting.Application.Common.Interfaces;
using Accounting.Domain.Entity;
using MediatR;

namespace Accounting.Application.BankCartDetails.Commands.CreateBankCartDetail;

/// <summary>
/// Constructs the <see cref="TB_BANKCARTDETAIL"/> Domain entity from the command, stages it via
/// <see cref="IBankCartDetailRepository"/>, and owns the transaction boundary by calling
/// <see cref="IUnitOfWork.SaveChangesAsync"/> exactly once. <c>ADDUSERID</c> is sourced from
/// <see cref="ICurrentUser"/> (the authenticated caller) — never from the request.
///
/// <c>ID</c> is always generated application-side (<see cref="Guid.NewGuid"/>).
/// </summary>
public sealed class CreateBankCartDetailCommandHandler : IRequestHandler<CreateBankCartDetailCommand, Guid>
{
    private readonly IBankCartDetailRepository _bankCartDetailRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUser _currentUser;

    public CreateBankCartDetailCommandHandler(
        IBankCartDetailRepository bankCartDetailRepository,
        IUnitOfWork unitOfWork,
        ICurrentUser currentUser)
    {
        _bankCartDetailRepository = bankCartDetailRepository;
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
    }

    public async Task<Guid> Handle(CreateBankCartDetailCommand request, CancellationToken cancellationToken)
    {
        var entity = new TB_BANKCARTDETAIL
        {
            ID = Guid.NewGuid(),
            RECEIP_ID = request.ReceipId,
            CHECK_ID = request.CheckId,
            BANK_ID = request.BankId,
            BRANCH_ID = request.BranchId,
            ACCOUNTNUMBER = request.AccountNumber,
            MONTH = request.Month,
            CHEQNO = request.Cheqno,
            RECIVDATE = request.RecivDate,
            CHECKRECEIPTTYPE = request.CheckReceiptType,
            DEBTOR = request.Debtor,
            CREDITOR = request.Creditor,
            VAHEDCODE = request.VahedCode,
            YEAR = request.Year,
            CHECK_INCORRENT_ID = request.CheckIncorrentId,
            ADDUSERID = _currentUser.UserId,
            CREATEDDATE = DateTime.UtcNow,
            ISDELETED = false,
        };

        await _bankCartDetailRepository.AddAsync(entity, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return entity.ID;
    }
}
