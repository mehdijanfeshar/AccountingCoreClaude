using Accounting.Application.Common.Exceptions;
using Accounting.Application.Common.Interfaces;
using MediatR;

namespace Accounting.Application.WorkShops.Commands.UpdateWorkShop;

/// <summary>
/// Loads the existing <see cref="Accounting.Domain.Entity.TB_WORKSHOP"/> row via
/// <see cref="IWorkShopRepository.GetForUpdateAsync"/> (change-tracked), overwrites every
/// writable field from the command, stamps audit columns, and owns the transaction boundary by
/// calling <see cref="IUnitOfWork.SaveChangesAsync"/> exactly once. Throws
/// <see cref="NotFoundException"/> — mapped to 404 by <c>GlobalExceptionHandler</c> — when the
/// row does not exist or is already soft-deleted. <c>ISDELETED</c> is <c>bool?</c> on this
/// table, so both <see langword="false"/> and <see langword="null"/> are treated as
/// "not deleted" — only an explicit <see langword="true"/> triggers 404, consistent with the
/// <c>ISDELETED != true</c> filter used by the read side. <c>CHANGEUSERID</c> is sourced from
/// <see cref="ICurrentUser"/> (the authenticated caller) — never from the request.
/// </summary>
public sealed class UpdateWorkShopCommandHandler : IRequestHandler<UpdateWorkShopCommand>
{
    private readonly IWorkShopRepository _workShopRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUser _currentUser;

    public UpdateWorkShopCommandHandler(
        IWorkShopRepository workShopRepository,
        IUnitOfWork unitOfWork,
        ICurrentUser currentUser)
    {
        _workShopRepository = workShopRepository;
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
    }

    public async Task Handle(UpdateWorkShopCommand request, CancellationToken cancellationToken)
    {
        var entity = await _workShopRepository.GetForUpdateAsync(request.Id, cancellationToken);

        if (entity is null || entity.ISDELETED == true)
        {
            throw new NotFoundException("WorkShop", request.Id);
        }

        entity.ACCOUNTCODE_ID = request.AccountCodeId;
        entity.BRANCH_ID = request.BranchId;
        entity.WORKSHOPNAME = request.WorkShopName;
        entity.WORKSHOPCODE = request.WorkShopCode;
        entity.VAHEDCODE = request.VahedCode;
        entity.ISACTIVE = request.IsActive;
        entity.CHECKFILE = request.CheckFile;
        entity.CHANGEUSERID = _currentUser.UserId;
        entity.UPDATEDDATE = DateTime.UtcNow;

        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
