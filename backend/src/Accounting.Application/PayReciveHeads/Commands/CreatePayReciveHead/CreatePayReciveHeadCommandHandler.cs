using Accounting.Application.Common.Interfaces;
using Accounting.Domain.Entity;
using MediatR;

namespace Accounting.Application.PayReciveHeads.Commands.CreatePayReciveHead;

/// <summary>
/// Constructs the <see cref="TB_PAYRECIVHEAD"/> Domain entity from the command, stages it via
/// <see cref="IPayReciveHeadRepository"/>, and owns the transaction boundary by calling
/// <see cref="IUnitOfWork.SaveChangesAsync"/> exactly once. <c>ADDUSERID</c> is sourced from
/// <see cref="ICurrentUser"/> (the authenticated caller) — never from the request — so it cannot
/// be forged by the client. On this table <c>ADDUSERID</c> and <c>CREATEDDATE</c> are both
/// NOT NULL in Legacy, so there is no "nullable audit column" caveat here (contrast
/// <c>TB_ELAMHEAD</c>/<c>TB_RABET</c>).
///
/// <c>ID</c> is always generated application-side (<see cref="Guid.NewGuid"/>) — this table has
/// no <c>sys_guid()</c> default in <c>LegacyDbContext</c>, but generating it here regardless
/// keeps this handler symmetric with every other Create handler in the project and never relies
/// on an Oracle DEFAULT.
///
/// <c>request.VahedCode</c> is equally unforgeable, just enforced one layer earlier: by the time
/// this handler runs, <c>VahedScopeBehavior</c> has already overwritten it with
/// <see cref="ICurrentUser.VahedCode"/>, so this handler can trust the field at face value and
/// simply map it onto <c>TB_PAYRECIVHEAD.VAHEDCODE</c> — it does not read
/// <see cref="ICurrentUser"/> directly for this field the way it does for <c>ADDUSERID</c>.
///
/// ⚠️ HEAD ONLY — no <c>TB_PAYRECIVDETAIL</c> row is ever created here. See
/// <see cref="CreatePayReciveHeadCommand"/> XML doc.
/// </summary>
public sealed class CreatePayReciveHeadCommandHandler : IRequestHandler<CreatePayReciveHeadCommand, Guid>
{
    private readonly IPayReciveHeadRepository _payReciveHeadRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUser _currentUser;

    public CreatePayReciveHeadCommandHandler(
        IPayReciveHeadRepository payReciveHeadRepository,
        IUnitOfWork unitOfWork,
        ICurrentUser currentUser)
    {
        _payReciveHeadRepository = payReciveHeadRepository;
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
    }

    public async Task<Guid> Handle(CreatePayReciveHeadCommand request, CancellationToken cancellationToken)
    {
        var entity = new TB_PAYRECIVHEAD
        {
            ID = Guid.NewGuid(),
            PAYRECIVCODE = request.PayReciveCode,
            PAYRECIVDATE = request.PayReciveDate,
            PAYRECIVDESCRIPTION = request.PayReciveDescription,
            PAYRECIVTYPE = request.PayReciveType,
            VAHEDCODE = request.VahedCode,
            YEAR = request.Year,
            VOUCHERSHEAD_ID = request.VoucherHeadId,
            ADDUSERID = _currentUser.UserId,
            CREATEDDATE = DateTime.UtcNow,
            ISDELETED = false,
        };

        await _payReciveHeadRepository.AddAsync(entity, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return entity.ID;
    }
}
