using Accounting.Application.Common.Exceptions;
using Accounting.Application.Common.Interfaces;
using MediatR;

namespace Accounting.Application.PersonActions.Commands.UpdatePersonAction;

/// <summary>
/// Loads the existing <see cref="Accounting.Domain.Entity.TB_PERSON_ACTION"/> row via
/// <see cref="IPersonActionRepository.GetForUpdateAsync"/> (change-tracked), overwrites every
/// writable field from the command, stamps audit columns, and owns the transaction boundary by
/// calling <see cref="IUnitOfWork.SaveChangesAsync"/> exactly once. Throws
/// <see cref="NotFoundException"/> when the row does not exist or is already soft-deleted.
/// </summary>
public sealed class UpdatePersonActionCommandHandler : IRequestHandler<UpdatePersonActionCommand>
{
    private readonly IPersonActionRepository _personActionRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUser _currentUser;

    public UpdatePersonActionCommandHandler(
        IPersonActionRepository personActionRepository,
        IUnitOfWork unitOfWork,
        ICurrentUser currentUser)
    {
        _personActionRepository = personActionRepository;
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
    }

    public async Task Handle(UpdatePersonActionCommand request, CancellationToken cancellationToken)
    {
        var entity = await _personActionRepository.GetForUpdateAsync(request.Id, cancellationToken);

        if (entity is null || entity.ISDELETED)
        {
            throw new NotFoundException("PersonAction", request.Id);
        }

        entity.USERNAME = request.UserName;
        entity.USERID = request.UserId;
        entity.FROMDATE = request.FromDate;
        entity.TODATE = request.ToDate;
        entity.STATUS = request.Status;
        entity.OPERATORROLE = request.OperatorRole;
        entity.VAHEDCODE = request.VahedCode;
        entity.CHANGEUSERID = _currentUser.UserId;
        entity.UPDATEDDATE = DateTime.UtcNow;

        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
