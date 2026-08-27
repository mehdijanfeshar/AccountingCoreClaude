using Accounting.Application.Common.Exceptions;
using Accounting.Application.Common.Interfaces;
using MediatR;

namespace Accounting.Application.BillLogs.Commands.UpdateBillLog;

/// <summary>
/// Loads the existing <see cref="Accounting.Domain.Entity.TB_BILL_LOG"/> row via
/// <see cref="IBillLogRepository.GetForUpdateAsync"/> (change-tracked), overwrites every
/// writable field from the command, stamps audit columns, and owns the transaction boundary by
/// calling <see cref="IUnitOfWork.SaveChangesAsync"/> exactly once. Throws
/// <see cref="NotFoundException"/> when the row does not exist or is already soft-deleted.
/// </summary>
public sealed class UpdateBillLogCommandHandler : IRequestHandler<UpdateBillLogCommand>
{
    private readonly IBillLogRepository _billLogRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUser _currentUser;

    public UpdateBillLogCommandHandler(
        IBillLogRepository billLogRepository,
        IUnitOfWork unitOfWork,
        ICurrentUser currentUser)
    {
        _billLogRepository = billLogRepository;
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
    }

    public async Task Handle(UpdateBillLogCommand request, CancellationToken cancellationToken)
    {
        var entity = await _billLogRepository.GetForUpdateAsync(request.Id, cancellationToken);

        if (entity is null || entity.ISDELETED)
        {
            throw new NotFoundException("BillLog", request.Id);
        }

        entity.LOG_DESC = request.LogDesc;
        entity.LOG_DATE = request.LogDate;
        entity.VAHEDCODE = request.VahedCode;
        entity.YEAR = request.Year;
        entity.CHANGEUSERID = _currentUser.UserId;
        entity.UPDATEDDATE = DateTime.UtcNow;

        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
