using Accounting.Application.Common.Interfaces;
using Accounting.Domain.Entity;
using MediatR;

namespace Accounting.Application.TafsilGroups.Commands.CreateTafsilGroup;

/// <summary>
/// Constructs the <see cref="TB_TAFSIL_GROUP"/> Domain entity from the command, stages it via
/// <see cref="ITafsilGroupRepository"/>, and owns the transaction boundary by calling
/// <see cref="IUnitOfWork.SaveChangesAsync"/> exactly once. <c>ADDUSERID</c> is sourced from
/// <see cref="ICurrentUser"/> (the authenticated caller) — never from the request — so it cannot
/// be forged by the client.
///
/// <c>ID</c> is always generated application-side (<see cref="Guid.NewGuid"/>); the Oracle
/// <c>ID</c> column has no <c>sys_guid()</c> default on this table.
/// </summary>
public sealed class CreateTafsilGroupCommandHandler : IRequestHandler<CreateTafsilGroupCommand, Guid>
{
    private readonly ITafsilGroupRepository _tafsilGroupRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUser _currentUser;

    public CreateTafsilGroupCommandHandler(
        ITafsilGroupRepository tafsilGroupRepository,
        IUnitOfWork unitOfWork,
        ICurrentUser currentUser)
    {
        _tafsilGroupRepository = tafsilGroupRepository;
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
    }

    public async Task<Guid> Handle(CreateTafsilGroupCommand request, CancellationToken cancellationToken)
    {
        var entity = new TB_TAFSIL_GROUP
        {
            ID = Guid.NewGuid(),
            TAFSILGROUP_CODE = request.TafsilGroupCode,
            TAFSILGROUP_NAME = request.TafsilGroupName,
            PERSONTYPE = request.PersonType,
            ADDUSERID = _currentUser.UserId,
            CREATEDDATE = DateTime.UtcNow,
            ISDELETED = false,
        };

        await _tafsilGroupRepository.AddAsync(entity, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return entity.ID;
    }
}
