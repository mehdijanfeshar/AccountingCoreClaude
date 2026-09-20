using Accounting.Application.Common.Exceptions;
using Accounting.Application.Common.Interfaces;
using Accounting.Application.IdentityHeads.Commands.Common;
using Accounting.Domain.Entity;
using MediatR;

namespace Accounting.Application.IdentityHeads.Commands.UpdateIdentityHead;

public sealed class UpdateIdentityHeadCommandHandler : IRequestHandler<UpdateIdentityHeadCommand>
{
    private readonly IIdentityHeadRepository _identityHeadRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUser _currentUser;

    public UpdateIdentityHeadCommandHandler(
        IIdentityHeadRepository identityHeadRepository,
        IUnitOfWork unitOfWork,
        ICurrentUser currentUser)
    {
        _identityHeadRepository = identityHeadRepository;
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
    }

    public async Task Handle(UpdateIdentityHeadCommand request, CancellationToken cancellationToken)
    {
        var entity = await _identityHeadRepository.GetForUpdateAsync(request.Id, cancellationToken);
        if (entity is null || entity.ISDELETED)
        {
            throw new NotFoundException("IdentityHead", request.Id);
        }

        entity.CHANGEUSERID = _currentUser.UserId;
        entity.UPDATEDDATE = DateTime.UtcNow;

        await ReconcileFixItemsAsync(entity, request, cancellationToken);

        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    /// <summary>
    /// Brings <c>TB_IDENTITYFIXITEMS</c> in line with the request's replacement set, matching
    /// existing rows on <c>IDENTITYSUBGRPS_ID</c> — the only identity a caller has for an item
    /// (see <see cref="IdentityHeadFixItemInput"/>), and the same key as the real UNIQUE
    /// constraint. Only stages work; the caller still owns the single
    /// <see cref="IUnitOfWork.SaveChangesAsync"/>, so the head and its items always move together.
    ///
    /// <para>
    /// <b>This differs from the tafsili-link reconcile it is modelled on.</b> A link has no payload
    /// beyond its key, so there a surviving row is left completely untouched. A fix item carries a
    /// <b>value</b>, so a surviving row whose value actually changed must be updated and
    /// re-stamped. An item whose value is unchanged is still left untouched, so an edit that only
    /// alters one field never rewrites the audit trail of the others.
    /// </para>
    /// </summary>
    private async Task ReconcileFixItemsAsync(
        TB_IDENTITYHEAD head,
        UpdateIdentityHeadCommand request,
        CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;
        var requested = request.FixItems ?? Array.Empty<IdentityHeadFixItemInput>();
        var existing = await _identityHeadRepository.GetActiveFixItemsAsync(request.Id, cancellationToken);

        var requestedBySubGroup = requested.ToDictionary(i => i.IdentitySubGroupId);

        foreach (var item in existing)
        {
            if (!requestedBySubGroup.TryGetValue(item.IDENTITYSUBGRPS_ID, out var match))
            {
                // Dropped from the replacement set — soft-deleted, never physically removed.
                item.ISDELETED = true;
                item.CHANGEUSERID = _currentUser.UserId;
                item.UPDATEDDATE = now;
                continue;
            }

            if (item.FIXITEMS_VALUE == match.Value)
            {
                continue;
            }

            item.FIXITEMS_VALUE = match.Value;
            item.CHANGEUSERID = _currentUser.UserId;
            item.UPDATEDDATE = now;
        }

        var existingSubGroups = existing
            .Select(i => i.IDENTITYSUBGRPS_ID)
            .ToHashSet();

        foreach (var item in requested)
        {
            if (existingSubGroups.Contains(item.IdentitySubGroupId))
            {
                continue;
            }

            // New items inherit their scope from the head, exactly as on the create path — never
            // from the request.
            await _identityHeadRepository.AddFixItemAsync(
                new TB_IDENTITYFIXITEM
                {
                    ID = Guid.NewGuid(),
                    IDENTITYHEAD_ID = head.ID,
                    IDENTITYSUBGRPS_ID = item.IdentitySubGroupId,
                    FIXITEMS_VALUE = item.Value,
                    VAHEDCODE = head.VAHEDCODE,
                    YEAR = head.YEAR,
                    ADDUSERID = _currentUser.UserId,
                    CREATEDDATE = now,
                    ISDELETED = false,
                },
                cancellationToken);
        }
    }
}
