using Accounting.Application.Common.Interfaces;
using Accounting.Domain.Entity;
using MediatR;

namespace Accounting.Application.WorkShops.Commands.CreateWorkShop;

/// <summary>
/// Constructs the <see cref="TB_WORKSHOP"/> Domain entity from the command, stages it via
/// <see cref="IWorkShopRepository"/>, and owns the transaction boundary by calling
/// <see cref="IUnitOfWork.SaveChangesAsync"/> exactly once. <c>ADDUSERID</c> is sourced from
/// <see cref="ICurrentUser"/> (the authenticated caller) — never from the request — so it
/// cannot be forged by the client, even though the column itself is nullable in Legacy.
///
/// <c>ID</c> is always generated application-side (<see cref="Guid.NewGuid"/>) — this table has
/// no <c>sys_guid()</c> default in <c>LegacyDbContext</c>, but generating it here regardless
/// keeps this handler symmetric with every other Create handler in the project and never relies
/// on an Oracle DEFAULT.
/// </summary>
public sealed class CreateWorkShopCommandHandler : IRequestHandler<CreateWorkShopCommand, Guid>
{
    private readonly IWorkShopRepository _workShopRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUser _currentUser;

    public CreateWorkShopCommandHandler(
        IWorkShopRepository workShopRepository,
        IUnitOfWork unitOfWork,
        ICurrentUser currentUser)
    {
        _workShopRepository = workShopRepository;
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
    }

    public async Task<Guid> Handle(CreateWorkShopCommand request, CancellationToken cancellationToken)
    {
        var entity = new TB_WORKSHOP
        {
            ID = Guid.NewGuid(),
            ACCOUNTCODE_ID = request.AccountCodeId,
            BRANCH_ID = request.BranchId,
            WORKSHOPNAME = request.WorkShopName,
            WORKSHOPCODE = request.WorkShopCode,
            VAHEDCODE = request.VahedCode,
            ISACTIVE = request.IsActive,
            CHECKFILE = request.CheckFile,
            ADDUSERID = _currentUser.UserId,
            CREATEDDATE = DateTime.UtcNow,
            ISDELETED = false,
        };

        await _workShopRepository.AddAsync(entity, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return entity.ID;
    }
}
