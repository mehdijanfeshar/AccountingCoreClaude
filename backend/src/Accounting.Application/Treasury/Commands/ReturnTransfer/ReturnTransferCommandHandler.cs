using Accounting.Application.Common.Interfaces;
using Accounting.Application.Treasury.Commands.Common;
using MediatR;

namespace Accounting.Application.Treasury.Commands.ReturnTransfer;

public sealed class ReturnTransferCommandHandler : IRequestHandler<ReturnTransferCommand>
{
    private readonly ITransferApprovalService _approvalService;
    private readonly IUnitOfWork _unitOfWork;

    public ReturnTransferCommandHandler(ITransferApprovalService approvalService, IUnitOfWork unitOfWork)
    {
        _approvalService = approvalService;
        _unitOfWork = unitOfWork;
    }

    public async Task Handle(ReturnTransferCommand request, CancellationToken cancellationToken)
    {
        await _approvalService.ReturnAsync(request.Id, request.VahedCode, request.Reason, cancellationToken);

        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
