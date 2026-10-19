using Accounting.Application.Common.Exceptions;
using Accounting.Domain.ValueObjects;
using Accounting.Application.Common.Interfaces;
using Accounting.Application.Commands.Vouchers.Common;
using Accounting.Application.OperationTemplates;
using MediatR;

namespace Accounting.Application.Commands.Vouchers.ChangeVoucherState;

public sealed class ChangeVoucherStateCommandHandler : IRequestHandler<ChangeVoucherStateCommand>
{
    private readonly IVoucherHeadRepository _voucherHeadRepository;
    private readonly IVoucherDetailRepository _voucherDetailRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUser _currentUser;
    private readonly IHesabyarVoucherStateHook? _hesabyarHook;

    public ChangeVoucherStateCommandHandler(
        IVoucherHeadRepository voucherHeadRepository,
        IVoucherDetailRepository voucherDetailRepository,
        IUnitOfWork unitOfWork,
        ICurrentUser currentUser,
        IHesabyarVoucherStateHook? hesabyarHook = null)
    {
        _voucherHeadRepository = voucherHeadRepository;
        _voucherDetailRepository = voucherDetailRepository;
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
        _hesabyarHook = hesabyarHook;
    }

    public async Task Handle(ChangeVoucherStateCommand request, CancellationToken cancellationToken)
    {
        var heads = await _voucherHeadRepository.GetManyForUpdateAsync(request.VoucherHeadIds, cancellationToken);

        // All-or-nothing on the batch. A partial move would leave the کارتابل showing some rows
        // in the new tab and some in the old, with nothing telling the user which is which —
        // worse than refusing. A missing id here means either a deleted voucher or a stale
        // selection; both deserve the same 404 the single-row paths give.
        var found = heads
            .Where(h => !(h.ISDELETED ?? false))
            .ToList();

        if (found.Count != request.VoucherHeadIds.Count)
        {
            var missing = request.VoucherHeadIds.First(id => found.All(h => h.ID != id));
            throw new NotFoundException("VoucherHead", missing);
        }

        // تأیید دائم is terminal — «دائم» is the point of the state. Checked for the whole batch
        // before anything is written, so the all-or-nothing contract above still holds: one
        // permanently-approved voucher in the selection refuses the entire move rather than
        // silently skipping that row.
        foreach (var head in found)
        {
            if (head.DOCLIFE == DocLife.Accepted)
            {
                throw new VoucherStateChangeDeniedException(head.ID);
            }
        }

        // Risk #3 (owner 2026-10-07): leaving یادداشت needs a balanced voucher with lines. Whole
        // batch checked before anything is written, same all-or-nothing contract as above.
        if (VoucherBalanceGuard.RequiresBalance(request.NewState))
        {
            await VoucherBalanceGuard.EnsureBalancedAsync(_voucherDetailRepository, found, cancellationToken);
        }

        // حسابیار (فاز ۶۰/۶۱): تفکیک وظایف G1 (ثبت‌کنندهٔ سند اجرا-دار نمی‌تواند «بررسی‌شده» کند، ۴۰۳ برای کل دسته)
        // و ثبت تاریخچهٔ بررسی/ارسال دوباره در همین تراکنش. روی سندهای بدون ردیف TB_OP_EXECUTION بی‌اثر است.
        if (_hesabyarHook is not null)
        {
            await _hesabyarHook.BeforeStateChangeAsync(found, request.NewState, cancellationToken);
        }

        var now = DateTime.UtcNow;

        foreach (var head in found)
        {
            head.DOCLIFE = request.NewState;
            head.CHANGEUSERID = _currentUser.UserId;
            head.UPDATEDDATE = now;
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
