using Accounting.Application.Common.Exceptions;
using Accounting.Application.Common.Interfaces;
using MediatR;

namespace Accounting.Application.BankCartDetails.Commands.UpdateBankCartDetail;

/// <summary>
/// Loads the existing <see cref="Accounting.Domain.Entity.TB_BANKCARTDETAIL"/> row via
/// <see cref="IBankCartDetailRepository.GetForUpdateAsync"/> (change-tracked), overwrites every
/// writable field from the command, stamps audit columns, and owns the transaction boundary by
/// calling <see cref="IUnitOfWork.SaveChangesAsync"/> exactly once. Throws
/// <see cref="NotFoundException"/> — mapped to 404 by <c>GlobalExceptionHandler</c> — when the
/// row does not exist or is already soft-deleted. <c>ISDELETED</c> is <c>bool?</c> on this table,
/// so both <see langword="false"/> and <see langword="null"/> are treated as "not deleted" — only
/// an explicit <see langword="true"/> triggers 404, consistent with the <c>ISDELETED != true</c>
/// filter used by the read side. <c>CHANGEUSERID</c> is sourced from <see cref="ICurrentUser"/> —
/// never from the request.
/// </summary>
public sealed class UpdateBankCartDetailCommandHandler : IRequestHandler<UpdateBankCartDetailCommand>
{
    private readonly IBankCartDetailRepository _bankCartDetailRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUser _currentUser;

    public UpdateBankCartDetailCommandHandler(
        IBankCartDetailRepository bankCartDetailRepository,
        IUnitOfWork unitOfWork,
        ICurrentUser currentUser)
    {
        _bankCartDetailRepository = bankCartDetailRepository;
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
    }

    public async Task Handle(UpdateBankCartDetailCommand request, CancellationToken cancellationToken)
    {
        var entity = await _bankCartDetailRepository.GetForUpdateAsync(request.Id, cancellationToken);

        if (entity is null || entity.ISDELETED == true)
        {
            throw new NotFoundException("BankCartDetail", request.Id);
        }

        entity.RECEIP_ID = request.ReceipId;
        entity.CHECK_ID = request.CheckId;
        entity.BANK_ID = request.BankId;
        entity.BRANCH_ID = request.BranchId;
        entity.ACCOUNTNUMBER = request.AccountNumber;
        entity.MONTH = request.Month;
        entity.CHEQNO = request.Cheqno;
        entity.RECIVDATE = request.RecivDate;
        entity.CHECKRECEIPTTYPE = request.CheckReceiptType;
        entity.DEBTOR = request.Debtor;
        entity.CREDITOR = request.Creditor;
        entity.VAHEDCODE = request.VahedCode;
        entity.YEAR = request.Year;
        entity.CHECK_INCORRENT_ID = request.CheckIncorrentId;
        entity.CHANGEUSERID = _currentUser.UserId;
        entity.UPDATEDDATE = DateTime.UtcNow;

        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
