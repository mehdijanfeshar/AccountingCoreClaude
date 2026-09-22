using Accounting.Application.Common.Interfaces;
using Accounting.Application.IdentityHeads.Commands.Common;
using Accounting.Domain.Entity;
using MediatR;

namespace Accounting.Application.IdentityHeads.Commands.CreateIdentityHead;

public sealed class CreateIdentityHeadCommandHandler : IRequestHandler<CreateIdentityHeadCommand, Guid>
{
    private readonly IIdentityHeadRepository _identityHeadRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUser _currentUser;

    public CreateIdentityHeadCommandHandler(
        IIdentityHeadRepository identityHeadRepository,
        IUnitOfWork unitOfWork,
        ICurrentUser currentUser)
    {
        _identityHeadRepository = identityHeadRepository;
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
    }

    public async Task<Guid> Handle(CreateIdentityHeadCommand request, CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;

        var serial = await _identityHeadRepository.GetNextSerialAsync(
            request.IdentityGroupId,
            request.VahedCode,
            request.Year,
            cancellationToken);

        var entity = new TB_IDENTITYHEAD
        {
            ID = Guid.NewGuid(),
            IDENTITYGROUPS_ID = request.IdentityGroupId,
            SERIAL = serial,
            VAHEDCODE = request.VahedCode,
            YEAR = request.Year,
            ADDUSERID = _currentUser.UserId,
            CREATEDDATE = now,
            ISDELETED = false,
        };

        await _identityHeadRepository.AddAsync(entity, cancellationToken);

        // Fix items travel with their head and are staged before the single SaveChanges below, so
        // a شناسنامه and its values are always persisted atomically. They inherit
        // IDENTITYHEAD_ID/VAHEDCODE/YEAR/ADDUSERID from the head written in this very call — never
        // from the request (see IdentityHeadFixItemInput).
        foreach (var item in request.FixItems ?? Array.Empty<IdentityHeadFixItemInput>())
        {
            await _identityHeadRepository.AddFixItemAsync(
                new TB_IDENTITYFIXITEM
                {
                    ID = Guid.NewGuid(),
                    IDENTITYHEAD_ID = entity.ID,
                    IDENTITYSUBGRPS_ID = item.IdentitySubGroupId,
                    FIXITEMS_VALUE = item.Value,
                    VAHEDCODE = request.VahedCode,
                    YEAR = request.Year,
                    ADDUSERID = _currentUser.UserId,
                    CREATEDDATE = now,
                    ISDELETED = false,
                },
                cancellationToken);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return entity.ID;
    }
}
