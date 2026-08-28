using Accounting.Application.Common.Exceptions;
using Accounting.Application.Common.Interfaces;
using MediatR;

namespace Accounting.Application.LevelTafsils.Commands.UpdateLevelTafsil;

/// <summary>
/// Loads the existing <see cref="Accounting.Domain.Entity.TB_LEVEL_TAFSIL"/> row via
/// <see cref="ILevelTafsilRepository.GetForUpdateAsync"/> (change-tracked), overwrites every
/// writable field from the command, stamps audit columns, and owns the transaction boundary by
/// calling <see cref="IUnitOfWork.SaveChangesAsync"/> exactly once. Throws
/// <see cref="NotFoundException"/> — mapped to 404 by <c>GlobalExceptionHandler</c> — when the
/// row does not exist or is already soft-deleted (<c>ISDELETED</c> is non-nullable <c>bool</c> on
/// this table, so a plain <see langword="true"/> check is sufficient). <c>CHANGEUSERID</c> is
/// sourced from <see cref="ICurrentUser"/> (the authenticated caller) — never from the request.
/// </summary>
public sealed class UpdateLevelTafsilCommandHandler : IRequestHandler<UpdateLevelTafsilCommand>
{
    private readonly ILevelTafsilRepository _levelTafsilRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUser _currentUser;

    public UpdateLevelTafsilCommandHandler(
        ILevelTafsilRepository levelTafsilRepository,
        IUnitOfWork unitOfWork,
        ICurrentUser currentUser)
    {
        _levelTafsilRepository = levelTafsilRepository;
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
    }

    public async Task Handle(UpdateLevelTafsilCommand request, CancellationToken cancellationToken)
    {
        var entity = await _levelTafsilRepository.GetForUpdateAsync(request.Id, cancellationToken);

        if (entity is null || entity.ISDELETED)
        {
            throw new NotFoundException("LevelTafsil", request.Id);
        }

        entity.LEVEL_CODE = request.LevelCode;
        entity.LEVEL_NAME = request.LevelName;
        entity.CHANGEUSERID = _currentUser.UserId;
        entity.UPDATEDDATE = DateTime.UtcNow;

        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
