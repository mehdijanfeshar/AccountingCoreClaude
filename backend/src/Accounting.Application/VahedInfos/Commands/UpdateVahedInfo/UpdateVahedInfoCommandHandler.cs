using Accounting.Application.Common.Exceptions;
using Accounting.Application.Common.Interfaces;
using MediatR;

namespace Accounting.Application.VahedInfos.Commands.UpdateVahedInfo;

/// <summary>
/// Loads the existing <see cref="Accounting.Domain.Entity.TB_VAHED_INFO"/> row via
/// <see cref="IVahedInfoRepository.GetForUpdateAsync"/> (change-tracked), overwrites every
/// writable field from the command, and owns the transaction boundary by calling
/// <see cref="IUnitOfWork.SaveChangesAsync"/> exactly once. Throws
/// <see cref="NotFoundException"/> — mapped to 404 by <c>GlobalExceptionHandler</c> — when the
/// row does not exist. There is no soft-deleted state to check here (no <c>ISDELETED</c>
/// column), so "row does not exist" is the ONLY 404 condition — mirrors
/// <c>UpdatePreDescribCommandHandler</c>.
///
/// Deliberately does NOT depend on <see cref="ICurrentUser"/>: this entity has no audit columns
/// at all to stamp. This is intentional, not an oversight — see
/// <see cref="UpdateVahedInfoCommand"/> XML doc.
/// </summary>
public sealed class UpdateVahedInfoCommandHandler : IRequestHandler<UpdateVahedInfoCommand>
{
    private readonly IVahedInfoRepository _vahedInfoRepository;
    private readonly IUnitOfWork _unitOfWork;

    public UpdateVahedInfoCommandHandler(
        IVahedInfoRepository vahedInfoRepository,
        IUnitOfWork unitOfWork)
    {
        _vahedInfoRepository = vahedInfoRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task Handle(UpdateVahedInfoCommand request, CancellationToken cancellationToken)
    {
        var entity = await _vahedInfoRepository.GetForUpdateAsync(request.Id, cancellationToken);

        if (entity is null)
        {
            throw new NotFoundException("VahedInfo", request.Id);
        }

        entity.VAHEDCODE = request.VahedCode;
        entity.VAHEDNAME = request.VahedName;
        entity.CITY_ID = request.CityId;
        entity.VAHEDTYPE_ID = request.VahedTypeId;
        entity.PARENT_ID = request.ParentId;

        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
