using Accounting.Application.Common.Exceptions;
using Accounting.Application.Common.Interfaces;
using MediatR;

namespace Accounting.Application.PreDescribs.Commands.UpdatePreDescrib;

/// <summary>
/// Loads the existing <see cref="Accounting.Domain.Entity.TB_PREDESCRIB"/> row via
/// <see cref="IPreDescribRepository.GetForUpdateAsync"/> (change-tracked), overwrites every
/// writable field from the command, and owns the transaction boundary by calling
/// <see cref="IUnitOfWork.SaveChangesAsync"/> exactly once. Throws
/// <see cref="NotFoundException"/> — mapped to 404 by <c>GlobalExceptionHandler</c> — when the
/// row does not exist. There is no soft-deleted state to check here (no <c>ISDELETED</c>
/// column), so "row does not exist" is the ONLY 404 condition, unlike every other Update
/// handler in this project.
///
/// Deliberately does NOT depend on <see cref="ICurrentUser"/>: this entity has no
/// <c>CHANGEUSERID</c>/<c>UPDATEDDATE</c> audit columns to stamp, and <c>ADDUSERID</c> is
/// creation audit — immutable here, never re-set on Update. This is intentional, not an
/// oversight.
/// </summary>
public sealed class UpdatePreDescribCommandHandler : IRequestHandler<UpdatePreDescribCommand>
{
    private readonly IPreDescribRepository _preDescribRepository;
    private readonly IUnitOfWork _unitOfWork;

    public UpdatePreDescribCommandHandler(
        IPreDescribRepository preDescribRepository,
        IUnitOfWork unitOfWork)
    {
        _preDescribRepository = preDescribRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task Handle(UpdatePreDescribCommand request, CancellationToken cancellationToken)
    {
        var entity = await _preDescribRepository.GetForUpdateAsync(request.Id, cancellationToken);

        if (entity is null)
        {
            throw new NotFoundException("PreDescrib", request.Id);
        }

        entity.ACCOUNTID = request.AccountId;
        entity.DESCRIP = request.Descrip;
        entity.VAHEDCODE = request.VahedCode;
        entity.FLAGVOUCHER = request.FlagVoucher;

        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
