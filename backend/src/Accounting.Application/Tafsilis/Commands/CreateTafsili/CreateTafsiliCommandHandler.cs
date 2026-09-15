using Accounting.Application.Common.Interfaces;
using Accounting.Domain.Entity;
using MediatR;

namespace Accounting.Application.Tafsilis.Commands.CreateTafsili;

/// <summary>
/// Constructs the <see cref="TB_TAFSILI"/> Domain entity from the command, stages it via
/// <see cref="ITafsiliRepository"/>, stages any requested گروه‌تفصیلی links, and owns the
/// transaction boundary by calling <see cref="IUnitOfWork.SaveChangesAsync"/> exactly once.
/// <c>ADDUSERID</c> is sourced from <see cref="ICurrentUser"/> (the authenticated caller) —
/// never from the request.
///
/// <c>request.VahedCode</c> is equally unforgeable, just enforced one layer earlier: by the time
/// this handler runs, <c>VahedScopeBehavior</c> has already overwritten it with
/// <see cref="ICurrentUser.VahedCode"/>, so this handler can trust the field at face value.
///
/// <b>Each <c>TB_TAFSIL_LINK_TAFSILGROUP</c> row created here gets <c>VAHEDCODE = request.VahedCode</c>
/// and <c>VAHEDTYPE = request.TafsilGroupLinkVahedType</c></b> (Rule B, <c>GetTafsiliLevelItemsQuery</c>
/// / <c>TafsiliLookupReadRepository</c>) — traced against the reference project
/// (<c>add-base-tafsili.component.ts</c> + <c>Tamin.Core.Entities.Tafsiliies.Tafsili.AddTafsiliLinkTafsilGroup</c>):
/// its form let the creator pick this value (defaulting to "All" for SETAD/admin-role callers,
/// forced to <see langword="null"/> otherwise). An earlier version of this handler hardcoded
/// <c>VAHEDTYPE = null</c> unconditionally, which is what made every link created through this API
/// invisible to every unit except the creator's own in the voucher-entry دینامیک تفصیلی lookup —
/// fixed 2026-09-13 once traced back to the reference project.
///
/// <c>ID</c> is always generated application-side (<see cref="Guid.NewGuid"/>).
/// </summary>
public sealed class CreateTafsiliCommandHandler : IRequestHandler<CreateTafsiliCommand, Guid>
{
    private readonly ITafsiliRepository _tafsiliRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUser _currentUser;

    public CreateTafsiliCommandHandler(
        ITafsiliRepository tafsiliRepository,
        IUnitOfWork unitOfWork,
        ICurrentUser currentUser)
    {
        _tafsiliRepository = tafsiliRepository;
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
    }

    public async Task<Guid> Handle(CreateTafsiliCommand request, CancellationToken cancellationToken)
    {
        var entity = new TB_TAFSILI
        {
            ID = Guid.NewGuid(),
            TAFSILI_CODE = request.TafsiliCode,
            TAFSILI_NAME = request.TafsiliName,
            TAFSIL_DESC = request.TafsilDesc,
            ISACTIVE = request.IsActive,
            PERSONTYPE = request.PersonType,
            OWNER = request.Owner,
            VAHEDTYPE = request.VahedType,
            VAHEDCODE = request.VahedCode,
            ADDUSERID = _currentUser.UserId,
            CREATEDDATE = DateTime.UtcNow,
            ISDELETED = false,
        };

        await _tafsiliRepository.AddAsync(entity, cancellationToken);

        short? groupLinkVahedType = request.TafsilGroupLinkVahedType is { } category ? (short)category : null;

        foreach (var tafsilGroupId in request.TafsilGroupIds)
        {
            await _tafsiliRepository.AddTafsiliGroupLinkAsync(
                new TB_TAFSIL_LINK_TAFSILGROUP
                {
                    ID = Guid.NewGuid(),
                    TAFSIL_ID = entity.ID,
                    TAFSILGROUP_ID = tafsilGroupId,
                    VAHEDCODE = request.VahedCode,
                    VAHEDTYPE = groupLinkVahedType,
                    ADDUSERID = _currentUser.UserId,
                    CREATEDDATE = DateTime.UtcNow,
                    ISDELETED = false,
                },
                cancellationToken);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return entity.ID;
    }
}
