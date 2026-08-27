using Accounting.Application.Common.Interfaces;
using Accounting.Domain.Entity;
using MediatR;

namespace Accounting.Application.AccountCodeInterfaces.Commands.CreateAccountCodeInterface;

/// <summary>
/// Constructs the <see cref="TB_ACCOUNTCODE_INTERFACE"/> Domain entity from the command, stages
/// it via <see cref="IAccountCodeInterfaceRepository"/>, and owns the transaction boundary by
/// calling <see cref="IUnitOfWork.SaveChangesAsync"/> exactly once. <c>ADDUSERID</c> is sourced
/// from <see cref="ICurrentUser"/> (the authenticated caller) — never from the request — so it
/// cannot be forged by the client.
/// </summary>
public sealed class CreateAccountCodeInterfaceCommandHandler
    : IRequestHandler<CreateAccountCodeInterfaceCommand, Guid>
{
    private readonly IAccountCodeInterfaceRepository _accountCodeInterfaceRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUser _currentUser;

    public CreateAccountCodeInterfaceCommandHandler(
        IAccountCodeInterfaceRepository accountCodeInterfaceRepository,
        IUnitOfWork unitOfWork,
        ICurrentUser currentUser)
    {
        _accountCodeInterfaceRepository = accountCodeInterfaceRepository;
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
    }

    public async Task<Guid> Handle(CreateAccountCodeInterfaceCommand request, CancellationToken cancellationToken)
    {
        var entity = new TB_ACCOUNTCODE_INTERFACE
        {
            ID = Guid.NewGuid(),
            TYPE = request.Type,
            ACCOUNTCODEID = request.AccountCodeId,
            ADDUSERID = _currentUser.UserId,
            CREATEDDATE = DateTime.UtcNow,
            ISDELETED = false,
        };

        await _accountCodeInterfaceRepository.AddAsync(entity, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return entity.ID;
    }
}
