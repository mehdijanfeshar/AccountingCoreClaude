using Accounting.Application.Common.Interfaces;
using Accounting.Domain.Entity;
using MediatR;

namespace Accounting.Application.BillLogs.Commands.CreateBillLog;

/// <summary>
/// Constructs the <see cref="TB_BILL_LOG"/> Domain entity from the command, stages it via
/// <see cref="IBillLogRepository"/>, and owns the transaction boundary by calling
/// <see cref="IUnitOfWork.SaveChangesAsync"/> exactly once. <c>ADDUSERID</c> is sourced from
/// <see cref="ICurrentUser"/> (the authenticated caller) — never from the request.
/// </summary>
public sealed class CreateBillLogCommandHandler : IRequestHandler<CreateBillLogCommand, Guid>
{
    private readonly IBillLogRepository _billLogRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUser _currentUser;

    public CreateBillLogCommandHandler(
        IBillLogRepository billLogRepository,
        IUnitOfWork unitOfWork,
        ICurrentUser currentUser)
    {
        _billLogRepository = billLogRepository;
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
    }

    public async Task<Guid> Handle(CreateBillLogCommand request, CancellationToken cancellationToken)
    {
        var entity = new TB_BILL_LOG
        {
            ID = Guid.NewGuid(),
            LOG_DESC = request.LogDesc,
            LOG_DATE = request.LogDate,
            VAHEDCODE = request.VahedCode,
            YEAR = request.Year,
            ADDUSERID = _currentUser.UserId,
            CREATEDDATE = DateTime.UtcNow,
            ISDELETED = false,
        };

        await _billLogRepository.AddAsync(entity, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return entity.ID;
    }
}
