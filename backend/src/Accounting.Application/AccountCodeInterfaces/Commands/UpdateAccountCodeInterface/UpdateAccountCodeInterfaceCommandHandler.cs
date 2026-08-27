using Accounting.Application.Common.Exceptions;
using Accounting.Application.Common.Interfaces;
using MediatR;

namespace Accounting.Application.AccountCodeInterfaces.Commands.UpdateAccountCodeInterface;

/// <summary>
/// Loads the existing <see cref="Accounting.Domain.Entity.TB_ACCOUNTCODE_INTERFACE"/> row via
/// <see cref="IAccountCodeInterfaceRepository.GetForUpdateAsync"/> (change-tracked), overwrites
/// every writable field from the command, stamps audit columns, and owns the transaction
/// boundary by calling <see cref="IUnitOfWork.SaveChangesAsync"/> exactly once. Throws
/// <see cref="NotFoundException"/> — mapped to 404 by <c>GlobalExceptionHandler</c> — when the
/// row does not exist or is already soft-deleted (<c>ISDELETED == true</c>).
/// </summary>
public sealed class UpdateAccountCodeInterfaceCommandHandler : IRequestHandler<UpdateAccountCodeInterfaceCommand>
{
    private readonly IAccountCodeInterfaceRepository _accountCodeInterfaceRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUser _currentUser;

    public UpdateAccountCodeInterfaceCommandHandler(
        IAccountCodeInterfaceRepository accountCodeInterfaceRepository,
        IUnitOfWork unitOfWork,
        ICurrentUser currentUser)
    {
        _accountCodeInterfaceRepository = accountCodeInterfaceRepository;
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
    }

    public async Task Handle(UpdateAccountCodeInterfaceCommand request, CancellationToken cancellationToken)
    {
        var entity = await _accountCodeInterfaceRepository.GetForUpdateAsync(request.Id, cancellationToken);

        if (entity is null || entity.ISDELETED)
        {
            throw new NotFoundException("AccountCodeInterface", request.Id);
        }

        entity.TYPE = request.Type;
        entity.ACCOUNTCODEID = request.AccountCodeId;
        entity.CHANGEUSERID = _currentUser.UserId;
        entity.UPDATEDDATE = DateTime.UtcNow;

        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
