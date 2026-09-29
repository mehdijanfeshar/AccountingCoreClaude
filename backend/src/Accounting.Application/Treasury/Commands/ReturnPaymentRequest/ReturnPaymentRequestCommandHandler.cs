using Accounting.Application.Common.Interfaces;
using Accounting.Application.Treasury.Commands.Common;
using MediatR;

namespace Accounting.Application.Treasury.Commands.ReturnPaymentRequest;

public sealed class ReturnPaymentRequestCommandHandler : IRequestHandler<ReturnPaymentRequestCommand>
{
    private readonly IPaymentRequestApprovalService _approvalService;
    private readonly IUnitOfWork _unitOfWork;

    public ReturnPaymentRequestCommandHandler(IPaymentRequestApprovalService approvalService, IUnitOfWork unitOfWork)
    {
        _approvalService = approvalService;
        _unitOfWork = unitOfWork;
    }

    public async Task Handle(ReturnPaymentRequestCommand request, CancellationToken cancellationToken)
    {
        await _approvalService.ReturnAsync(request.Id, request.VahedCode, request.Reason, cancellationToken);

        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
