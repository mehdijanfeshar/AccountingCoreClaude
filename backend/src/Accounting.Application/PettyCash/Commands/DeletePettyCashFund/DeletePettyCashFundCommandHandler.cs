using Accounting.Application.Common.Exceptions;
using Accounting.Application.Common.Interfaces;
using MediatR;

namespace Accounting.Application.PettyCash.Commands.DeletePettyCashFund;

/// <summary>
/// Loads the existing <see cref="Accounting.Domain.Entity.TB_PC_FUND"/> row via
/// <see cref="IPettyCashFundRepository.GetForUpdateAsync"/> and soft-deletes it — refusing (409)
/// when the fund still has at least one non-deleted صورت‌هزینه. Idempotency: a row already
/// soft-deleted (<c>ISDELETED == true</c>) is a no-op success, mirroring
/// <c>DeleteRevolvingFundCommandHandler</c>.
/// </summary>
public sealed class DeletePettyCashFundCommandHandler : IRequestHandler<DeletePettyCashFundCommand>
{
    private readonly IPettyCashFundRepository _pettyCashFundRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUser _currentUser;

    public DeletePettyCashFundCommandHandler(
        IPettyCashFundRepository pettyCashFundRepository,
        IUnitOfWork unitOfWork,
        ICurrentUser currentUser)
    {
        _pettyCashFundRepository = pettyCashFundRepository;
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
    }

    public async Task Handle(DeletePettyCashFundCommand request, CancellationToken cancellationToken)
    {
        var entity = await _pettyCashFundRepository.GetForUpdateAsync(request.Id, request.VahedCode, cancellationToken);

        if (entity is null)
        {
            throw new NotFoundException("PettyCashFund", request.Id);
        }

        if (entity.ISDELETED)
        {
            return;
        }

        var hasActiveExpenseDocs = await _pettyCashFundRepository.HasActiveExpenseDocsAsync(request.Id, cancellationToken);

        if (hasActiveExpenseDocs)
        {
            throw new PettyCashFundHasExpenseDocsException(request.Id);
        }

        entity.ISDELETED = true;
        entity.CHANGEUSERID = _currentUser.UserId;
        entity.UPDATEDDATE = DateTime.UtcNow;

        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
