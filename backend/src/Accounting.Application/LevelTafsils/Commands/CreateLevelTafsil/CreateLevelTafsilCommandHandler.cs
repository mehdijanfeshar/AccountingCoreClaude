using Accounting.Application.Common.Interfaces;
using Accounting.Domain.Entity;
using MediatR;

namespace Accounting.Application.LevelTafsils.Commands.CreateLevelTafsil;

/// <summary>
/// Constructs the <see cref="TB_LEVEL_TAFSIL"/> Domain entity from the command, stages it via
/// <see cref="ILevelTafsilRepository"/>, and owns the transaction boundary by calling
/// <see cref="IUnitOfWork.SaveChangesAsync"/> exactly once. <c>ADDUSERID</c> is sourced from
/// <see cref="ICurrentUser"/> (the authenticated caller) — never from the request — so it cannot
/// be forged by the client.
///
/// <c>ID</c> is always generated application-side (<see cref="Guid.NewGuid"/>); the Oracle
/// <c>ID</c> column has no <c>sys_guid()</c> default on this table.
/// </summary>
public sealed class CreateLevelTafsilCommandHandler : IRequestHandler<CreateLevelTafsilCommand, Guid>
{
    private readonly ILevelTafsilRepository _levelTafsilRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUser _currentUser;

    public CreateLevelTafsilCommandHandler(
        ILevelTafsilRepository levelTafsilRepository,
        IUnitOfWork unitOfWork,
        ICurrentUser currentUser)
    {
        _levelTafsilRepository = levelTafsilRepository;
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
    }

    public async Task<Guid> Handle(CreateLevelTafsilCommand request, CancellationToken cancellationToken)
    {
        var entity = new TB_LEVEL_TAFSIL
        {
            ID = Guid.NewGuid(),
            LEVEL_CODE = request.LevelCode,
            LEVEL_NAME = request.LevelName,
            ADDUSERID = _currentUser.UserId,
            CREATEDDATE = DateTime.UtcNow,
            ISDELETED = false,
        };

        await _levelTafsilRepository.AddAsync(entity, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return entity.ID;
    }
}
