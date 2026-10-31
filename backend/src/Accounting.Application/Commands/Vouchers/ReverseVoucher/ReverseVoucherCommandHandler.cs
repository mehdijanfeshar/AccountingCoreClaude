using Accounting.Application.Common.Interfaces;
using MediatR;

namespace Accounting.Application.Commands.Vouchers.ReverseVoucher;

/// <summary>
/// Builds the mirrored voucher. Returns the new head's <c>ID</c>.
///
/// <para>
/// <b>Three deliberate departures from the reference implementation</b>
/// (<c>AddReversVoucherCommandHabdler</c>), each noted at the line it affects in
/// <see cref="VoucherReverser"/>: it copies <c>IsDeleted</c> from the source (so reversing a
/// deleted voucher produced a deleted reversal), it reads the source without any unit scoping,
/// and it has no editability guard — which here is correct and kept, but for a different and
/// explicit reason.
/// </para>
/// <para>
/// فاز ۶۶: منطق به <see cref="VoucherReverser"/> رفت تا زمان‌بندی تعهدی هم همان را صدا بزند؛ رفتار این endpoint
/// (امروز، «یادداشت»، سال سند مبدأ) عوض نشده.
/// </para>
/// </summary>
public sealed class ReverseVoucherCommandHandler : IRequestHandler<ReverseVoucherCommand, Guid>
{
    private readonly VoucherReverser _reverser;
    private readonly IUnitOfWork _unitOfWork;

    public ReverseVoucherCommandHandler(
        IVoucherHeadRepository voucherHeadRepository,
        IVoucherDetailRepository voucherDetailRepository,
        IUnitOfWork unitOfWork,
        ICurrentUser currentUser)
    {
        _reverser = new VoucherReverser(voucherHeadRepository, voucherDetailRepository, currentUser);
        _unitOfWork = unitOfWork;
    }

    public async Task<Guid> Handle(ReverseVoucherCommand request, CancellationToken cancellationToken)
    {
        var (voucherId, _) = await _reverser.ReverseAsync(request.Id, request.VahedCode, options: null, cancellationToken);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return voucherId;
    }
}
