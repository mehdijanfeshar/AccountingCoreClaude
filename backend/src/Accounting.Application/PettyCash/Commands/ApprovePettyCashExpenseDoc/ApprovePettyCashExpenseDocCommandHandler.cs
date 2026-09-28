using Accounting.Application.Common.Interfaces;
using Accounting.Application.PettyCash.Commands.Common;
using MediatR;

namespace Accounting.Application.PettyCash.Commands.ApprovePettyCashExpenseDoc;

/// <summary>
/// تکمیل بخش ۲ (۲۰۲۶-۰۹-۲۸): این endpoint اکنون «تأیید نهایی» است، نه گذار تک‌مرحله‌ای قدیمی —
/// منطق واقعی در <see cref="IPettyCashFinalApprovalService"/> است (verified-check، سقف اختیار،
/// SoD دوم در برابر بررس)، همان سرویسی که <c>BulkApprovePettyCashExpenseDocsCommandHandler</c>
/// هم استفاده می‌کند.
/// </summary>
public sealed class ApprovePettyCashExpenseDocCommandHandler : IRequestHandler<ApprovePettyCashExpenseDocCommand>
{
    private readonly IPettyCashFinalApprovalService _finalApprovalService;
    private readonly IUnitOfWork _unitOfWork;

    public ApprovePettyCashExpenseDocCommandHandler(
        IPettyCashFinalApprovalService finalApprovalService,
        IUnitOfWork unitOfWork)
    {
        _finalApprovalService = finalApprovalService;
        _unitOfWork = unitOfWork;
    }

    public async Task Handle(ApprovePettyCashExpenseDocCommand request, CancellationToken cancellationToken)
    {
        await _finalApprovalService.FinalApproveAsync(request.Id, request.VahedCode, request.Note, cancellationToken);

        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
