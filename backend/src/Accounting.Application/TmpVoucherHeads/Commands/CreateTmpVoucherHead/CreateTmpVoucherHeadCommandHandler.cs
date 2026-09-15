using Accounting.Application.Common.Interfaces;
using Accounting.Domain.Entity;
using MediatR;

namespace Accounting.Application.TmpVoucherHeads.Commands.CreateTmpVoucherHead;

/// <summary>
/// Constructs the <see cref="TB_TMP_VOUCHERHEAD"/> Domain entity from the command, stages it via
/// <see cref="ITmpVoucherHeadRepository"/>, and owns the transaction boundary by calling
/// <see cref="IUnitOfWork.SaveChangesAsync"/> exactly once. <c>ADDUSERID</c> is sourced from
/// <see cref="ICurrentUser"/> (the authenticated caller) — never from the request — so it cannot
/// be forged by the client, even though all four audit columns are nullable in Legacy on this
/// table (mirroring <c>TB_ELAMHEAD</c> and <c>TB_RABET</c>).
///
/// <c>ID</c> is always generated application-side (<see cref="Guid.NewGuid"/>) — this table has
/// no <c>sys_guid()</c> default in <c>LegacyDbContext</c>, but generating it here regardless
/// keeps this handler symmetric with every other Create handler in the project and never relies
/// on an Oracle DEFAULT.
///
/// ⚠️ HEAD ONLY — no <c>TB_TMP_VOUCHERSDETAIL</c> row is ever created here, so every temporary
/// voucher created through this path starts (and stays) empty. See
/// <see cref="CreateTmpVoucherHeadCommand"/> XML doc for why that diverges from the reference
/// project, which creates head and details as one graph.
///
/// <c>request.VahedCode</c> is equally unforgeable, just enforced one layer earlier: by the
/// time this handler runs, <c>VahedScopeBehavior</c> has already overwritten it with
/// <see cref="ICurrentUser.VahedCode"/>, so this handler can trust the field at face value and
/// simply map it onto <c>TB_TMP_VOUCHERHEAD.VAHEDCODE</c> — it does not read
/// <see cref="ICurrentUser"/> directly for this field the way it does for <c>ADDUSERID</c>.
/// </summary>
public sealed class CreateTmpVoucherHeadCommandHandler : IRequestHandler<CreateTmpVoucherHeadCommand, Guid>
{
    private readonly ITmpVoucherHeadRepository _tmpVoucherHeadRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUser _currentUser;

    public CreateTmpVoucherHeadCommandHandler(
        ITmpVoucherHeadRepository tmpVoucherHeadRepository,
        IUnitOfWork unitOfWork,
        ICurrentUser currentUser)
    {
        _tmpVoucherHeadRepository = tmpVoucherHeadRepository;
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
    }

    public async Task<Guid> Handle(CreateTmpVoucherHeadCommand request, CancellationToken cancellationToken)
    {
        var entity = new TB_TMP_VOUCHERHEAD
        {
            ID = Guid.NewGuid(),
            VOUCHERSHEAD_ID = request.VoucherHeadId,
            DATE_DOC = request.DateDoc,
            HEAD_DESC = request.HeadDesc,
            VAHEDCODE = request.VahedCode,
            YEAR = request.Year,
            SYS_TYPE = request.SysType,
            SOURCEID = request.SourceId,
            ADDUSERID = _currentUser.UserId,
            CREATEDDATE = DateTime.UtcNow,
            ISDELETED = false,
        };

        await _tmpVoucherHeadRepository.AddAsync(entity, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return entity.ID;
    }
}
