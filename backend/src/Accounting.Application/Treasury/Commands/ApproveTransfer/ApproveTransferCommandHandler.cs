using Accounting.Application.Common.Interfaces;
using Accounting.Application.Treasury.Commands.Common;
using MediatR;

namespace Accounting.Application.Treasury.Commands.ApproveTransfer;

/// <summary>
/// Sensitive multi-write use case (GL voucher + state transition) — wrapped in an explicit
/// <see cref="IUnitOfWork.BeginTransactionAsync"/>/<see cref="IUnitOfWork.CommitTransactionAsync"/>
/// pair per CLAUDE.md's rule, same shape as <c>RegisterReceiptCommandHandler</c>.
/// </summary>
public sealed class ApproveTransferCommandHandler : IRequestHandler<ApproveTransferCommand>
{
    private readonly ITransferApprovalService _approvalService;
    private readonly IUnitOfWork _unitOfWork;

    public ApproveTransferCommandHandler(ITransferApprovalService approvalService, IUnitOfWork unitOfWork)
    {
        _approvalService = approvalService;
        _unitOfWork = unitOfWork;
    }

    public async Task Handle(ApproveTransferCommand request, CancellationToken cancellationToken)
    {
        await _unitOfWork.BeginTransactionAsync(cancellationToken);

        try
        {
            await _approvalService.ApproveAsync(request.Id, request.VahedCode, request.BankReference, cancellationToken);

            await _unitOfWork.SaveChangesAsync(cancellationToken);
            await _unitOfWork.CommitTransactionAsync(cancellationToken);
        }
        catch
        {
            await _unitOfWork.RollbackTransactionAsync(cancellationToken);
            throw;
        }
    }
}
