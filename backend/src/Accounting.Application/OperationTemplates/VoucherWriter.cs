using System.Globalization;
using Accounting.Application.Common.Interfaces;
using Accounting.Application.Vouchers.Commands.Common;
using Accounting.Domain.Entity;
using Accounting.Domain.ValueObjects;

namespace Accounting.Application.OperationTemplates;

/// <summary>
/// <see cref="VoucherDraft"/> ⇒ سند Legacy (<c>TB_VOUCHERSHEAD</c> + <c>TB_VOUCHERSDETAIL</c> +
/// <c>TB_VOUCHERDETAIL_LINK_TAFSILI</c>) از همان مسیری که سندهای خودکار خزانه‌داری/تنخواه می‌روند
/// (<c>ReceiptVoucherBuilder</c>): شمارهٔ بعدی از <see cref="IVoucherHeadRepository.GetNextDocNumAsync"/>،
/// گارد «تفصیلی الزامی» <see cref="IVoucherTafsiliLevelGuard"/> برای هر ردیف، <c>DOCLIFE=Temporary</c>
/// و <c>ISAUTOMATIC=true</c>.
///
/// چرا <c>CreateVoucherHeadCommand</c> نه: آن Command تفصیلی ردیف را نمی‌پذیرد (ریسک #۲۱) و شمارهٔ
/// سند را از فراخوان می‌خواهد؛ و ارسال آن از داخل هندلر یک SaveChanges جدا می‌زند که اتمیک بودن
/// سند + <c>OperationExecution</c> را می‌شکند.
///
/// هیچ‌چیز ذخیره نمی‌شود — فقط stage؛ <c>ExecuteOperationHandler</c> یک بار SaveChanges می‌زند.
/// </summary>
public sealed class VoucherWriter : IVoucherWriter
{
    private readonly IVoucherHeadRepository _voucherHeadRepository;
    private readonly IVoucherDetailRepository _voucherDetailRepository;
    private readonly IVoucherTafsiliLevelGuard _tafsiliLevelGuard;
    private readonly IVoucherChequeService _chequeService;
    private readonly IVoucherLineExtrasService _extrasService;
    private readonly ICurrentUser _currentUser;

    public VoucherWriter(
        IVoucherHeadRepository voucherHeadRepository,
        IVoucherDetailRepository voucherDetailRepository,
        IVoucherTafsiliLevelGuard tafsiliLevelGuard,
        IVoucherChequeService chequeService,
        IVoucherLineExtrasService extrasService,
        ICurrentUser currentUser)
    {
        _voucherHeadRepository = voucherHeadRepository;
        _voucherDetailRepository = voucherDetailRepository;
        _tafsiliLevelGuard = tafsiliLevelGuard;
        _chequeService = chequeService;
        _extrasService = extrasService;
        _currentUser = currentUser;
    }

    public async Task<(Guid VoucherId, string VoucherNo)> CreateDraftVoucherAsync(VoucherDraft draft, CancellationToken ct)
    {
        // کمربند دوم: موتور تراز را قطعی کرده؛ اینجا فقط نمی‌گذاریم سند نامتراز هرگز stage شود.
        if (draft.TotalDebit != draft.TotalCredit || draft.TotalDebit <= 0)
            throw new InvalidOperationException("سند پیش‌نویس الگوی عملیات تراز نیست.");

        var linksByLine = draft.Lines
            .Select(l => l.Details.Select(d => new VoucherDetailTafsiliLinkInput(d.DetailId, d.LevelId)).ToList())
            .ToList();

        for (var i = 0; i < draft.Lines.Count; i++)
            await _tafsiliLevelGuard.EnsureSatisfiedAsync(draft.Lines[i].SubsidiaryAccountId, linksByLine[i], ct);

        var dateDoc = ToJalali(draft.VoucherDate);
        var year = dateDoc[..4];
        var docNum = await _voucherHeadRepository.GetNextDocNumAsync(draft.VahedCode, year, ct);

        var now = DateTime.UtcNow;
        var userId = _currentUser.UserId;

        var head = new TB_VOUCHERSHEAD
        {
            ID = Guid.NewGuid(),
            DOC_NUM = docNum,
            DATE_DOC = dateDoc,
            DOCLIFE = DocLife.Temporary,
            SYSTEM_TYPE = draft.SystemTypeId,
            HEAD_DESC = Truncate(draft.Description, 250),
            APENDIX = string.IsNullOrWhiteSpace(draft.Apendix) ? null : Truncate(draft.Apendix.Trim(), 800),
            VAHEDCODE = draft.VahedCode,
            YEAR = year,
            ISAUTOMATIC = true,
            ADDUSERID = userId,
            CREATEDDATE = now,
            ISDELETED = false,
        };

        await _voucherHeadRepository.AddAsync(head, ct);

        for (var i = 0; i < draft.Lines.Count; i++)
        {
            var line = draft.Lines[i];
            var detailId = Guid.NewGuid();

            // چک ردیف از همان سرویس فرم سند: ابطال/استفادهٔ تکراری/چاپ‌شده را رد می‌کند و چک صوری می‌سازد.
            var checkId = line.CheckId is not null || line.Cheque?.SoriCheckBookId is not null
                ? await _chequeService.ApplyAsync(line.CheckId, detailId, checkChanged: true, line.Cheque, draft.VahedCode, ct)
                : null;

            var detail = new TB_VOUCHERSDETAIL
            {
                ID = detailId,
                CHECK_ID = checkId,
                VOUCHERSHEAD_ID = head.ID,
                ACCOUNT_ID = line.SubsidiaryAccountId,
                DESCRIPTION = Truncate(line.Description, 200),
                RADIF = i + 1,
                DEBTOR = line.Debit,
                CREDITOR = line.Credit,
                VAHEDCODE = draft.VahedCode,
                YEAR = year,
                ADDUSERID = userId,
                CREATEDDATE = now,
                ISDELETED = false,
            };

            await _voucherDetailRepository.AddAsync(detail, ct);

            foreach (var link in linksByLine[i])
            {
                await _voucherDetailRepository.AddTafsiliLinkAsync(new TB_VOUCHERDETAIL_LINK_TAFSILI
                {
                    ID = Guid.NewGuid(),
                    VOUCHERSDETAIL_ID = detail.ID,
                    TAFSILI_ID = link.TafsiliId,
                    LEVEL_ID = link.LevelId,
                    VAHEDCODE = draft.VahedCode,
                    YEAR = year,
                    ADDUSERID = userId,
                    CREATEDDATE = now,
                    ISDELETED = false,
                }, ct);
            }

            // شناسه/ویژگی/فیش — همان قاعدهٔ فرم سند؛ الزامی‌ها اینجا هم دوباره کنترل می‌شوند.
            await _extrasService.ApplyAsync(detail, linksByLine[i].Select(l => l.TafsiliId).ToList(), line.Extras, replace: false, ct);
        }

        return (head.ID, docNum);
    }

    /// <summary>DATE_DOC قالب Legacy: <c>yyyyMMdd</c> شمسی.</summary>
    private static string ToJalali(DateOnly date)
    {
        var calendar = new PersianCalendar();
        var d = date.ToDateTime(TimeOnly.MinValue);
        return $"{calendar.GetYear(d):0000}{calendar.GetMonth(d):00}{calendar.GetDayOfMonth(d):00}";
    }

    private static string Truncate(string s, int max) => s.Length <= max ? s : s[..max];
}
