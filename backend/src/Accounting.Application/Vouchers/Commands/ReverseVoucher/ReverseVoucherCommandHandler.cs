using System.Globalization;
using Accounting.Application.Common.Exceptions;
using Accounting.Application.Common.Interfaces;
using Accounting.Domain.Entity;
using Accounting.Domain.ValueObjects;
using MediatR;

namespace Accounting.Application.Vouchers.Commands.ReverseVoucher;

/// <summary>
/// Builds the mirrored voucher. Returns the new head's <c>ID</c>.
///
/// <para>
/// <b>Three deliberate departures from the reference implementation</b>
/// (<c>AddReversVoucherCommandHabdler</c>), each noted at the line it affects:
/// it copies <c>IsDeleted</c> from the source (so reversing a deleted voucher produced a deleted
/// reversal), it reads the source without any unit scoping, and it has no editability guard —
/// which here is correct and kept, but for a different and explicit reason.
/// </para>
/// </summary>
public sealed class ReverseVoucherCommandHandler : IRequestHandler<ReverseVoucherCommand, Guid>
{
    private readonly IVoucherHeadRepository _voucherHeadRepository;
    private readonly IVoucherDetailRepository _voucherDetailRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUser _currentUser;

    public ReverseVoucherCommandHandler(
        IVoucherHeadRepository voucherHeadRepository,
        IVoucherDetailRepository voucherDetailRepository,
        IUnitOfWork unitOfWork,
        ICurrentUser currentUser)
    {
        _voucherHeadRepository = voucherHeadRepository;
        _voucherDetailRepository = voucherDetailRepository;
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
    }

    public async Task<Guid> Handle(ReverseVoucherCommand request, CancellationToken cancellationToken)
    {
        // Unit-scoped, unlike the reference's unscoped GetByIdAsync: you may only reverse a
        // voucher you can already see.
        var source = await _voucherHeadRepository.GetForUpdateAsync(request.Id, request.VahedCode, cancellationToken);

        if (source is null || source.ISDELETED == true)
        {
            throw new NotFoundException("VoucherHead", request.Id);
        }

        // NOTE: deliberately NO VoucherEditability check. Reversal does not modify the source —
        // it creates a new draft beside it — and reversing a تأیید دائم voucher is the entire
        // point of the operation. Phase 38's lock is about changing a finalized voucher, which
        // this does not do.

        var lines = await _voucherDetailRepository.GetActiveByHeadAsync(
            source.ID,
            request.VahedCode,
            cancellationToken);

        var nextDocNum = await _voucherHeadRepository.GetNextDocNumAsync(
            request.VahedCode,
            source.YEAR,
            cancellationToken);

        var now = DateTime.UtcNow;
        var userId = _currentUser.UserId;

        var head = new TB_VOUCHERSHEAD
        {
            ID = Guid.NewGuid(),
            DOC_NUM = nextDocNum,
            DATE_DOC = TodayJalali(),
            // Always a draft, whatever the source was: a reversal is a new document that has not
            // been reviewed by anyone yet.
            DOCLIFE = DocLife.Draft,
            HEAD_DESC = $"معکوس سند شماره {source.DOC_NUM}",
            APENDIX = source.APENDIX,
            SYSTEM_TYPE = source.SYSTEM_TYPE,
            FLAG_STATE = source.FLAG_STATE,
            ISAUTOMATIC = source.ISAUTOMATIC,
            VAHEDCODE = request.VahedCode,
            YEAR = source.YEAR,
            // The link back to what this reverses. The reference sets the same field, and it is
            // the only record that these two documents belong together.
            PARENTHEAD_ID = source.ID,
            // NOT copied from the source. The reference does `SetIsDeleted(voucher.IsDeleted)`,
            // which would produce an already-deleted reversal for a soft-deleted source — a new
            // document is never born deleted.
            ISDELETED = false,
            ADDUSERID = userId,
            CREATEDDATE = now,
        };

        await _voucherHeadRepository.AddAsync(head, cancellationToken);

        var radif = 0;

        foreach (var line in lines)
        {
            // Lines tied to a receipt or a cheque are skipped, exactly as the reference does:
            // those carry a link to a real payment instrument that must not be duplicated onto a
            // second document.
            if (line.RECEIP_ID is not null || line.CHECK_ID is not null)
            {
                continue;
            }

            radif++;

            var reversed = new TB_VOUCHERSDETAIL
            {
                ID = Guid.NewGuid(),
                VOUCHERSHEAD_ID = head.ID,
                ACCOUNT_ID = line.ACCOUNT_ID,
                DESCRIPTION = line.DESCRIPTION,
                RADIF = radif,
                // The reversal itself: the two sides trade places.
                DEBTOR = line.CREDITOR,
                CREDITOR = line.DEBTOR,
                LOWLEVELCODE_ID = line.LOWLEVELCODE_ID,
                ETEBAR_ID = line.ETEBAR_ID,
                VAHEDCODE = request.VahedCode,
                YEAR = line.YEAR,
                ISDELETED = false,
                ADDUSERID = userId,
                CREATEDDATE = now,
            };

            await _voucherDetailRepository.AddAsync(reversed, cancellationToken);

            // تفصیلی assignments carry over unchanged — reversing an amount does not change which
            // cost centre or party it belonged to.
            var tafsiliLinks = await _voucherDetailRepository.GetActiveTafsiliLinksAsync(line.ID, cancellationToken);

            foreach (var link in tafsiliLinks)
            {
                await _voucherDetailRepository.AddTafsiliLinkAsync(
                    new TB_VOUCHERDETAIL_LINK_TAFSILI
                    {
                        ID = Guid.NewGuid(),
                        VOUCHERSDETAIL_ID = reversed.ID,
                        TAFSILI_ID = link.TAFSILI_ID,
                        LEVEL_ID = link.LEVEL_ID,
                        ISDELETED = false,
                        ADDUSERID = userId,
                        CREATEDDATE = now,
                    },
                    cancellationToken);
            }
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return head.ID;
    }

    /// <summary>
    /// Today as a Legacy <c>YYYYMMDD</c> Jalali string — the format every <c>DATE_DOC</c> in this
    /// schema uses. Same approach as the reference project.
    /// </summary>
    private static string TodayJalali()
    {
        var calendar = new PersianCalendar();
        var now = DateTime.Now;

        return $"{calendar.GetYear(now):0000}{calendar.GetMonth(now):00}{calendar.GetDayOfMonth(now):00}";
    }
}
