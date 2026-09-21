using Accounting.Application.Common.Exceptions;
using Accounting.Application.Common.Interfaces;
using MediatR;

namespace Accounting.Application.IdentityHeads.Commands.DeleteIdentityHead;

public sealed class DeleteIdentityHeadCommandHandler : IRequestHandler<DeleteIdentityHeadCommand>
{
    private readonly IIdentityHeadRepository _identityHeadRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUser _currentUser;

    public DeleteIdentityHeadCommandHandler(
        IIdentityHeadRepository identityHeadRepository,
        IUnitOfWork unitOfWork,
        ICurrentUser currentUser)
    {
        _identityHeadRepository = identityHeadRepository;
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
    }

    public async Task Handle(DeleteIdentityHeadCommand request, CancellationToken cancellationToken)
    {
        var entity = await _identityHeadRepository.GetForUpdateAsync(request.Id, request.VahedCode, cancellationToken);
        if (entity is null)
        {
            throw new NotFoundException("IdentityHead", request.Id);
        }

        if (entity.ISDELETED)
        {
            return;
        }

        var now = DateTime.UtcNow;

        entity.ISDELETED = true;
        entity.CHANGEUSERID = _currentUser.UserId;
        entity.UPDATEDDATE = now;

        // Cascade to the embedded fix items, in the same transaction. Without this they would
        // survive their parent and keep occupying their slot in AK_AK_IDENTYFIXITEMS_IDENTYFI —
        // the exact orphan-rows problem phase 9 fixed for voucher lines.
        var fixItems = await _identityHeadRepository.GetActiveFixItemsAsync(entity.ID, cancellationToken);
        foreach (var item in fixItems)
        {
            item.ISDELETED = true;
            item.CHANGEUSERID = _currentUser.UserId;
            item.UPDATEDDATE = now;
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
