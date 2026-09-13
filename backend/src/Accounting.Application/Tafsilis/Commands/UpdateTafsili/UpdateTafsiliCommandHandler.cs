using Accounting.Application.Common.Exceptions;
using Accounting.Application.Common.Interfaces;
using Accounting.Domain.Entity;
using MediatR;

namespace Accounting.Application.Tafsilis.Commands.UpdateTafsili;

/// <summary>
/// Loads the existing <see cref="TB_TAFSILI"/> row via <see cref="ITafsiliRepository.GetForUpdateAsync"/>
/// (change-tracked), overwrites every writable field from the command, reconciles the
/// گروه‌تفصیلی link set, stamps audit columns, and owns the transaction boundary by calling
/// <see cref="IUnitOfWork.SaveChangesAsync"/> exactly once. Throws <see cref="NotFoundException"/>
/// — mapped to 404 by <c>GlobalExceptionHandler</c> — when the row does not exist or is already
/// soft-deleted (<c>ISDELETED</c> is nullable <c>bool</c> here, so both <see langword="null"/> and
/// <see langword="false"/> mean "not deleted", only an explicit <see langword="true"/> blocks the
/// update). <c>CHANGEUSERID</c> is sourced from <see cref="ICurrentUser"/>, never the request.
/// <c>request.VahedCode</c> is likewise unforgeable, enforced one layer earlier: by the time this
/// handler runs, <c>VahedScopeBehavior</c> has already overwritten it with
/// <see cref="ICurrentUser.VahedCode"/>, so this handler simply maps it onto <c>TB_TAFSILI.VAHEDCODE</c>
/// at face value, re-stamping it on every update exactly like <c>UpdateExpenseCommandHandler</c>
/// — see <c>UpdateTafsiliCommand</c> XML doc "Scope note" for what this does and does not cover.
///
/// <b>Link reconciliation:</b> loads the current, active links via
/// <see cref="ITafsiliRepository.GetTafsiliGroupLinksAsync"/> (change-tracked), soft-deletes any
/// whose <c>TAFSILGROUP_ID</c> is no longer present in <see cref="UpdateTafsiliCommand.TafsilGroupIds"/>,
/// and adds a new row (via <see cref="ITafsiliRepository.AddTafsiliGroupLinkAsync"/>) for any
/// requested id that has no existing active link — exactly the same shape as
/// <c>UpdateVoucherDetailCommandHandler</c>'s تفصیلی-link reconciliation. New rows get the same
/// <c>VAHEDCODE</c>/<c>VAHEDTYPE</c> defaults as <c>CreateTafsiliCommandHandler</c> — see that
/// class's XML doc for the rationale.
/// </summary>
public sealed class UpdateTafsiliCommandHandler : IRequestHandler<UpdateTafsiliCommand>
{
    private readonly ITafsiliRepository _tafsiliRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUser _currentUser;

    public UpdateTafsiliCommandHandler(
        ITafsiliRepository tafsiliRepository,
        IUnitOfWork unitOfWork,
        ICurrentUser currentUser)
    {
        _tafsiliRepository = tafsiliRepository;
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
    }

    public async Task Handle(UpdateTafsiliCommand request, CancellationToken cancellationToken)
    {
        var entity = await _tafsiliRepository.GetForUpdateAsync(request.Id, cancellationToken);

        if (entity is null || entity.ISDELETED == true)
        {
            throw new NotFoundException("Tafsili", request.Id);
        }

        entity.TAFSILI_CODE = request.TafsiliCode;
        entity.TAFSILI_NAME = request.TafsiliName;
        entity.TAFSIL_DESC = request.TafsilDesc;
        entity.ISACTIVE = request.IsActive;
        entity.PERSONTYPE = request.PersonType;
        entity.OWNER = request.Owner;
        entity.VAHEDTYPE = request.VahedType;
        entity.VAHEDCODE = request.VahedCode;
        entity.CHANGEUSERID = _currentUser.UserId;
        entity.UPDATEDDATE = DateTime.UtcNow;

        var existingLinks = await _tafsiliRepository.GetTafsiliGroupLinksAsync(request.Id, cancellationToken);
        var requestedGroupIds = new HashSet<Guid>(request.TafsilGroupIds);
        var existingGroupIds = new HashSet<Guid>(existingLinks.Select(l => l.TAFSILGROUP_ID));

        foreach (var link in existingLinks)
        {
            if (!requestedGroupIds.Contains(link.TAFSILGROUP_ID))
            {
                link.ISDELETED = true;
                link.CHANGEUSERID = _currentUser.UserId;
                link.UPDATEDDATE = DateTime.UtcNow;
            }
        }

        foreach (var tafsilGroupId in requestedGroupIds)
        {
            if (existingGroupIds.Contains(tafsilGroupId))
            {
                continue;
            }

            await _tafsiliRepository.AddTafsiliGroupLinkAsync(
                new TB_TAFSIL_LINK_TAFSILGROUP
                {
                    ID = Guid.NewGuid(),
                    TAFSIL_ID = request.Id,
                    TAFSILGROUP_ID = tafsilGroupId,
                    VAHEDCODE = entity.VAHEDCODE ?? _currentUser.VahedCode ?? string.Empty,
                    VAHEDTYPE = null,
                    ADDUSERID = _currentUser.UserId,
                    CREATEDDATE = DateTime.UtcNow,
                    ISDELETED = false,
                },
                cancellationToken);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
