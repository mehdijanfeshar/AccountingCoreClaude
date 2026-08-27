using Accounting.Application.Common.Exceptions;
using Accounting.Application.Common.Interfaces;
using MediatR;

namespace Accounting.Application.AccountExceptions.Commands.UpdateAccountException;

/// <summary>
/// Loads the existing <see cref="Accounting.Domain.Entity.TB_ACCOUNTEXCEPTION"/> row via
/// <see cref="IAccountExceptionRepository.GetForUpdateAsync"/> (change-tracked), overwrites
/// every writable field from the command, stamps audit columns, and owns the transaction
/// boundary by calling <see cref="IUnitOfWork.SaveChangesAsync"/> exactly once. Throws
/// <see cref="NotFoundException"/> — mapped to 404 by <c>GlobalExceptionHandler</c> — when the
/// row does not exist or is already soft-deleted.
/// </summary>
public sealed class UpdateAccountExceptionCommandHandler : IRequestHandler<UpdateAccountExceptionCommand>
{
    private readonly IAccountExceptionRepository _accountExceptionRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUser _currentUser;

    public UpdateAccountExceptionCommandHandler(
        IAccountExceptionRepository accountExceptionRepository,
        IUnitOfWork unitOfWork,
        ICurrentUser currentUser)
    {
        _accountExceptionRepository = accountExceptionRepository;
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
    }

    public async Task Handle(UpdateAccountExceptionCommand request, CancellationToken cancellationToken)
    {
        var entity = await _accountExceptionRepository.GetForUpdateAsync(request.Id, cancellationToken);

        if (entity is null || entity.ISDELETED)
        {
            throw new NotFoundException("AccountException", request.Id);
        }

        entity.ACCOUNTCOE_ID = request.AccountCoeId;
        entity.VAHEDTYPE_ID = request.VahedTypeId;
        entity.CHANGEUSERID = _currentUser.UserId;
        entity.UPDATEDDATE = DateTime.UtcNow;

        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
