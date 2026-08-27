using Accounting.Application.Common.Interfaces;
using Accounting.Domain.Entity;
using MediatR;

namespace Accounting.Application.PreDescribs.Commands.CreatePreDescrib;

/// <summary>
/// Constructs the <see cref="TB_PREDESCRIB"/> Domain entity from the command, stages it via
/// <see cref="IPreDescribRepository"/>, and owns the transaction boundary by calling
/// <see cref="IUnitOfWork.SaveChangesAsync"/> exactly once. <c>ADDUSERID</c> is sourced from
/// <see cref="ICurrentUser"/> (the authenticated caller) — never from the request — so it
/// cannot be forged by the client. There is no <c>CREATEDDATE</c> column on this entity to
/// stamp (unlike <c>TB_ACCOUNTCODE</c>/<c>TB_VOUCHERSHEAD</c>), so none is invented here.
///
/// <c>ID</c> is always generated application-side (<see cref="Guid.NewGuid"/>); the Oracle
/// <c>ID</c> column has no <c>sys_guid()</c> default on this particular table, so that risk
/// (see CLAUDE.md) does not apply here — noted only for symmetry with the other three entities
/// in this batch, which do carry that default.
/// </summary>
public sealed class CreatePreDescribCommandHandler : IRequestHandler<CreatePreDescribCommand, Guid>
{
    private readonly IPreDescribRepository _preDescribRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUser _currentUser;

    public CreatePreDescribCommandHandler(
        IPreDescribRepository preDescribRepository,
        IUnitOfWork unitOfWork,
        ICurrentUser currentUser)
    {
        _preDescribRepository = preDescribRepository;
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
    }

    public async Task<Guid> Handle(CreatePreDescribCommand request, CancellationToken cancellationToken)
    {
        var entity = new TB_PREDESCRIB
        {
            ID = Guid.NewGuid(),
            ACCOUNTID = request.AccountId,
            DESCRIP = request.Descrip,
            ADDUSERID = _currentUser.UserId,
            VAHEDCODE = request.VahedCode,
            FLAGVOUCHER = request.FlagVoucher,
        };

        await _preDescribRepository.AddAsync(entity, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return entity.ID;
    }
}
