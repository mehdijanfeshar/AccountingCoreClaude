using System.Globalization;
using Accounting.Application.Common.Exceptions;
using Accounting.Application.Common.Interfaces;
using Accounting.Domain.Entity;
using Accounting.Domain.ValueObjects;

namespace Accounting.Application.Commands.Vouchers.ReverseVoucher;

/// <summary>
/// گزینه‌های سند معکوس. پیش‌فرض‌ها = رفتار دکمهٔ «معکوس سند» (فاز ۳۹): تاریخ امروز، «یادداشت»، سال سند مبدأ.
/// </summary>
/// <param name="DateDoc">تاریخ شمسی <c>yyyyMMdd</c>؛ وقتی داده شود سال و شمارهٔ سند هم از همین تاریخ می‌آیند.</param>
/// <param name="RequireBalanced">سند ساخته‌شده باید تراز و غیرصفر باشد (ردیف‌های چک/فیش کپی نمی‌شوند).</param>
public sealed record VoucherReverseOptions(
    string? DateDoc = null,
    DocLife DocLife = DocLife.Draft,
    string? HeadDescription = null,
    bool? IsAutomatic = null,
    bool RequireBalanced = false);

/// <summary>
/// منطق مشترک معکوس سند — هم <see cref="ReverseVoucherCommandHandler"/> و هم اجرای زمان‌بندی تعهدی (فاز ۶۶).
/// فقط stage می‌کند؛ SaveChanges با فراخوان.
/// </summary>
public sealed class VoucherReverser
{
    private readonly IVoucherHeadRepository _voucherHeadRepository;
    private readonly IVoucherDetailRepository _voucherDetailRepository;
    private readonly ICurrentUser _currentUser;

    public VoucherReverser(
        IVoucherHeadRepository voucherHeadRepository,
        IVoucherDetailRepository voucherDetailRepository,
        ICurrentUser currentUser)
    {
        _voucherHeadRepository = voucherHeadRepository;
        _voucherDetailRepository = voucherDetailRepository;
        _currentUser = currentUser;
    }

    public async Task<(Guid VoucherId, string VoucherNo)> ReverseAsync(
        Guid sourceId,
        string vahedCode,
        VoucherReverseOptions? options,
        CancellationToken cancellationToken)
    {
        options ??= new VoucherReverseOptions();

        // Unit-scoped, unlike the reference's unscoped GetByIdAsync: you may only reverse a
        // voucher you can already see.
        var source = await _voucherHeadRepository.GetForUpdateAsync(sourceId, vahedCode, cancellationToken);

        if (source is null || source.ISDELETED == true)
        {
            throw new NotFoundException("VoucherHead", sourceId);
        }

        // NOTE: deliberately NO VoucherEditability check. Reversal does not modify the source —
        // it creates a new draft beside it — and reversing a تأیید دائم voucher is the entire
        // point of the operation. Phase 38's lock is about changing a finalized voucher, which
        // this does not do.

        var lines = await _voucherDetailRepository.GetActiveByHeadAsync(
            source.ID,
            vahedCode,
            cancellationToken);

        // تاریخ صریح (زمان‌بندی تعهدی) ⇒ سال و شماره از همان تاریخ؛ وگرنه رفتار قبلی: امروز و سال سند مبدأ.
        var dateDoc = options.DateDoc ?? TodayJalali();
        var year = options.DateDoc is null ? source.YEAR : options.DateDoc[..4];

        var nextDocNum = await _voucherHeadRepository.GetNextDocNumAsync(
            vahedCode,
            year,
            cancellationToken);

        var now = DateTime.UtcNow;
        var userId = _currentUser.UserId;

        var head = new TB_VOUCHERSHEAD
        {
            ID = Guid.NewGuid(),
            DOC_NUM = nextDocNum,
            DATE_DOC = dateDoc,
            // Draft by default, whatever the source was: a reversal is a new document that has not
            // been reviewed by anyone yet. The accrual schedule asks for «موقت» like other automatic vouchers.
            DOCLIFE = options.DocLife,
            HEAD_DESC = options.HeadDescription ?? $"معکوس سند شماره {source.DOC_NUM}",
            APENDIX = source.APENDIX,
            SYSTEM_TYPE = source.SYSTEM_TYPE,
            FLAG_STATE = source.FLAG_STATE,
            ISAUTOMATIC = options.IsAutomatic ?? source.ISAUTOMATIC,
            VAHEDCODE = vahedCode,
            YEAR = year,
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
        decimal totalDebit = 0, totalCredit = 0;

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
            totalDebit += line.CREDITOR ?? 0;
            totalCredit += line.DEBTOR ?? 0;

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
                VAHEDCODE = vahedCode,
                YEAR = options.DateDoc is null ? line.YEAR : year,
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

        // تراز سند در دامنه نیست (ریسک #۳): سند خودکار پیش از ذخیره اینجا کنترل می‌شود.
        if (options.RequireBalanced && (totalDebit != totalCredit || totalDebit <= 0))
        {
            throw new BusinessRuleException(
                $"برگشت سند شماره {source.DOC_NUM} تراز نمی‌شود (ردیف چک یا فیش دارد یا مبلغ ندارد)؛ آن را دستی برگردانید و این سطر را لغو کنید.");
        }

        return (head.ID, nextDocNum);
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
