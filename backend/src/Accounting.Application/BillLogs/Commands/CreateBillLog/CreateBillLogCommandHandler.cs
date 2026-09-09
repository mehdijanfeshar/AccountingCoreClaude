using Accounting.Application.Common.Interfaces;
using Accounting.Domain.Entity;
using MediatR;

namespace Accounting.Application.BillLogs.Commands.CreateBillLog;

/// <summary>
/// Constructs the <see cref="TB_BILL_LOG"/> Domain entity from the command, stages it via
/// <see cref="IBillLogRepository"/>, and owns the transaction boundary by calling
/// <see cref="IUnitOfWork.SaveChangesAsync"/> exactly once. <c>ADDUSERID</c> is sourced from
/// <see cref="ICurrentUser"/> (the authenticated caller) — never from the request.
///
/// <c>request.VahedCode</c> is equally unforgeable, just enforced one layer earlier: by the time
/// this handler runs, <c>VahedScopeBehavior</c> has already overwritten it with
/// <see cref="ICurrentUser.VahedCode"/>, so this handler can trust the field at face value and
/// simply map it onto <c>TB_BILL_LOG.VAHEDCODE</c> — it does not read <see cref="ICurrentUser"/>
/// directly for this field the way it does for <c>ADDUSERID</c>.
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
