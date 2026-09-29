using Accounting.Application.Common.Interfaces;
using Accounting.Application.Treasury.Commands.Common;
using MediatR;

namespace Accounting.Application.Treasury.Commands.RejectTransfer;

public sealed class RejectTransferCommandHandler : IRequestHandler<RejectTransferCommand>
{
    private readonly ITransferApprovalService _approvalService;
    private readonly IUnitOfWork _unitOfWork;

    public RejectTransferCommandHandler(ITransferApprovalService approvalService, IUnitOfWork unitOfWork)
    {
        _approvalService = approvalService;
        _unitOfWork = unitOfWork;
    }

    public async Task Handle(RejectTransferCommand request, CancellationToken cancellationToken)
    {
        await _approvalService.RejectAsync(request.Id, request.VahedCode, request.Reason, cancellationToken);

        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
