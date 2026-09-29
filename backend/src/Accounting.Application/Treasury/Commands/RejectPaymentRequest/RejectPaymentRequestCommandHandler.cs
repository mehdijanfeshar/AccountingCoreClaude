using Accounting.Application.Common.Interfaces;
using Accounting.Application.Treasury.Commands.Common;
using MediatR;

namespace Accounting.Application.Treasury.Commands.RejectPaymentRequest;

public sealed class RejectPaymentRequestCommandHandler : IRequestHandler<RejectPaymentRequestCommand>
{
    private readonly IPaymentRequestApprovalService _approvalService;
    private readonly IUnitOfWork _unitOfWork;

    public RejectPaymentRequestCommandHandler(IPaymentRequestApprovalService approvalService, IUnitOfWork unitOfWork)
    {
        _approvalService = approvalService;
        _unitOfWork = unitOfWork;
    }

    public async Task Handle(RejectPaymentRequestCommand request, CancellationToken cancellationToken)
    {
        await _approvalService.RejectAsync(request.Id, request.VahedCode, request.Reason, cancellationToken);

        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
