using Accounting.Application.Common.Exceptions;
using Accounting.Application.Common.Interfaces;
using MediatR;

namespace Accounting.Application.TafsilGroups.Commands.UpdateTafsilGroup;

/// <summary>
/// Loads the existing <see cref="Accounting.Domain.Entity.TB_TAFSIL_GROUP"/> row via
/// <see cref="ITafsilGroupRepository.GetForUpdateAsync"/> (change-tracked), overwrites every
/// writable field from the command, stamps audit columns, and owns the transaction boundary by
/// calling <see cref="IUnitOfWork.SaveChangesAsync"/> exactly once. Throws
/// <see cref="NotFoundException"/> — mapped to 404 by <c>GlobalExceptionHandler</c> — when the
/// row does not exist or is already soft-deleted (<c>ISDELETED</c> is non-nullable <c>bool</c> on
/// this table, so a plain <see langword="true"/> check is sufficient). <c>CHANGEUSERID</c> is
/// sourced from <see cref="ICurrentUser"/> (the authenticated caller) — never from the request.
/// </summary>
public sealed class UpdateTafsilGroupCommandHandler : IRequestHandler<UpdateTafsilGroupCommand>
{
    private readonly ITafsilGroupRepository _tafsilGroupRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUser _currentUser;

    public UpdateTafsilGroupCommandHandler(
        ITafsilGroupRepository tafsilGroupRepository,
        IUnitOfWork unitOfWork,
        ICurrentUser currentUser)
    {
        _tafsilGroupRepository = tafsilGroupRepository;
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
    }

    public async Task Handle(UpdateTafsilGroupCommand request, CancellationToken cancellationToken)
    {
        var entity = await _tafsilGroupRepository.GetForUpdateAsync(request.Id, cancellationToken);

        if (entity is null || entity.ISDELETED)
        {
            throw new NotFoundException("TafsilGroup", request.Id);
        }

        entity.TAFSILGROUP_CODE = request.TafsilGroupCode;
        entity.TAFSILGROUP_NAME = request.TafsilGroupName;
        entity.PERSONTYPE = request.PersonType;
        entity.CHANGEUSERID = _currentUser.UserId;
        entity.UPDATEDDATE = DateTime.UtcNow;

        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
