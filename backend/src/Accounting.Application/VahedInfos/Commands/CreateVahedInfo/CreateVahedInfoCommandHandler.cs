using Accounting.Application.Common.Interfaces;
using Accounting.Domain.Entity;
using MediatR;

namespace Accounting.Application.VahedInfos.Commands.CreateVahedInfo;

/// <summary>
/// Constructs the <see cref="TB_VAHED_INFO"/> Domain entity from the command, stages it via
/// <see cref="IVahedInfoRepository"/>, and owns the transaction boundary by calling
/// <see cref="IUnitOfWork.SaveChangesAsync"/> exactly once.
///
/// Deliberately does NOT depend on <see cref="ICurrentUser"/>: <c>TB_VAHED_INFO</c> has no
/// <c>ADDUSERID</c> (or any other audit) column to stamp — there is nothing for
/// <see cref="ICurrentUser"/> to feed. This means writes to this table leave no audit trail at
/// all; that is a recorded gap (see command XML doc), not something this handler should paper
/// over by inventing a column.
///
/// <c>ID</c> is always generated application-side (<see cref="Guid.NewGuid"/>); the Oracle
/// <c>ID</c> column has a <c>sys_guid()</c> default, but that default is therefore never
/// exercised — avoiding the recorded 🔴 CLAUDE.md risk where <c>sys_guid()</c> yields a
/// non-dashed value that the project's strict <c>GuidToChar36Converter</c> would reject on read.
/// </summary>
public sealed class CreateVahedInfoCommandHandler : IRequestHandler<CreateVahedInfoCommand, Guid>
{
    private readonly IVahedInfoRepository _vahedInfoRepository;
    private readonly IUnitOfWork _unitOfWork;

    public CreateVahedInfoCommandHandler(
        IVahedInfoRepository vahedInfoRepository,
        IUnitOfWork unitOfWork)
    {
        _vahedInfoRepository = vahedInfoRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Guid> Handle(CreateVahedInfoCommand request, CancellationToken cancellationToken)
    {
        var entity = new TB_VAHED_INFO
        {
            ID = Guid.NewGuid(),
            VAHEDCODE = request.VahedCode,
            VAHEDNAME = request.VahedName,
            CITY_ID = request.CityId,
            VAHEDTYPE_ID = request.VahedTypeId,
            PARENT_ID = request.ParentId,
        };

        await _vahedInfoRepository.AddAsync(entity, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return entity.ID;
    }
}
