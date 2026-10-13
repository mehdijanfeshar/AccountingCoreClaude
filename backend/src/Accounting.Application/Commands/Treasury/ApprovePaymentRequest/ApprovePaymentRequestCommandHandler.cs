using Accounting.Application.Common.Interfaces;
using Accounting.Application.Commands.Treasury.Common;
using MediatR;

namespace Accounting.Application.Commands.Treasury.ApprovePaymentRequest;

public sealed class ApprovePaymentRequestCommandHandler : IRequestHandler<ApprovePaymentRequestCommand>
{
    private readonly IPaymentRequestApprovalService _approvalService;
    private readonly IUnitOfWork _unitOfWork;

    public ApprovePaymentRequestCommandHandler(IPaymentRequestApprovalService approvalService, IUnitOfWork unitOfWork)
    {
        _approvalService = approvalService;
        _unitOfWork = unitOfWork;
    }

    public async Task Handle(ApprovePaymentRequestCommand request, CancellationToken cancellationToken)
    {
        await _approvalService.ApproveAsync(request.Id, request.VahedCode, request.Note, isBulkApprove: false, cancellationToken);

        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
