using Accounting.Application.Common.Exceptions;
using Accounting.Application.Common.Interfaces;
using Accounting.Application.Vouchers.Commands.Common;
using Accounting.Domain.Entity;
using Accounting.Domain.ValueObjects;

namespace Accounting.Application.Elams;

/// <summary>
/// منطق مشترک گردش اعلامیه — ثبت ردیف‌ها، صدور سند و ساخت خودکار اعلامیهٔ رسیده در واحد مقصد.
/// همه‌چیز فقط stage می‌شود؛ ذخیره یک‌جا در Handler.
///
/// <para>
/// <b>عین پروژهٔ مرجع</b> (تصمیم صاحب پروژه ۲۰۲۶-۱۰-۰۵):
/// <list type="bullet">
/// <item>ماهیت «بدهکار» ⇒ ردیف‌ها بستانکار و حساب رابط بدهکار؛ «بستانکار» برعکس
/// (<c>AddOtherElamHeadCommandHandler.CreateElamDetail</c>).</item>
/// <item>تأیید نهایی صادره ⇒ اعلامیهٔ رسیده و سندش خودکار در واحد مقصد، با همان حساب‌ها، تفصیلی‌ها و
/// همان جهت مبالغ فرستنده (<c>AddElamReciveVahedCommandHandler</c>).</item>
/// <item>شناسهٔ ردیف رابط: «کد واحد طرف + 3 + پنج رقم آخر سریال» (<c>SendElamVoucherCommandHandler</c>).</item>
/// </list>
/// اصلاح‌های آگاهانه نسبت به مرجع: سند با تاریخ اعلامیه (نه تاریخ روز) صادر می‌شود تا در سال مالی
/// خودش بنشیند؛ سند موقت و خودکار است؛ ردیف‌های اعلامیهٔ رسیده کد واحد گیرنده را می‌گیرند
/// (مرجع کد فرستنده را کپی می‌کرد)؛ سند رسیده سال و واحد دارد (مرجع خالی می‌گذاشت).
/// </para>
/// </summary>
public sealed class ElamWorkflowService
{
    private readonly IElamWorkflowRepository _repository;
    private readonly IVoucherHeadRepository _voucherHeadRepository;
    private readonly IVoucherDetailRepository _voucherDetailRepository;
    private readonly IVoucherTafsiliLevelGuard _tafsiliLevelGuard;
    private readonly ICurrentUser _currentUser;

    public ElamWorkflowService(
        IElamWorkflowRepository repository,
        IVoucherHeadRepository voucherHeadRepository,
        IVoucherDetailRepository voucherDetailRepository,
        IVoucherTafsiliLevelGuard tafsiliLevelGuard,
        ICurrentUser currentUser)
    {
        _repository = repository;
        _voucherHeadRepository = voucherHeadRepository;
        _voucherDetailRepository = voucherDetailRepository;
        _tafsiliLevelGuard = tafsiliLevelGuard;
        _currentUser = currentUser;
    }

    public static ElamKind KindOf(byte? webStat) => webStat switch
    {
        >= 1 and <= 4 => ElamKind.Revenue,
        >= 9 and <= 10 => ElamKind.Received,
        _ => ElamKind.Sent,
    };

    public static string RabetTypeCode(ElamKind kind) => ((int)kind).ToString();

    /// <summary>ویرایش/حذف فقط پیش از صدور سند و فقط برای اعلامیه‌ای که این واحد ساخته (نه رسیده).</summary>
    public static bool IsEditable(TB_ELAMHEAD head)
        => head.VOUCHERSHEAD_ID is null
            && (head.WEB_STAT == (byte)ElamWebStat.CreateOther || head.WEB_STAT == (byte)ElamWebStat.CreateDramad);

    public async Task<ElamRabetAccount> GetRabetAsync(ElamKind kind, CancellationToken cancellationToken)
        => await _repository.GetRabetAccountAsync(RabetTypeCode(kind), cancellationToken)
            ?? throw new ElamConflictException(
                $"حساب رابط «{KindLabel(kind)}» در جدول حساب‌های رابط (نوع {RabetTypeCode(kind)}) تعریف نشده است.");

    public static string KindLabel(ElamKind kind) => kind switch
    {
        ElamKind.Received => "اعلامیهٔ رسیده",
        ElamKind.Revenue => "اعلامیهٔ صادرهٔ درآمد",
        _ => "اعلامیهٔ صادره",
    };

    /// <summary>ردیف‌ها را با قاعدهٔ تفصیلی الزامی/مجاز بررسی و stage می‌کند.</summary>
    public async Task StageDetailsAsync(
        TB_ELAMHEAD head, IReadOnlyList<ElamDetailInput> details, CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;
        var userId = _currentUser.UserId;
        foreach (var input in details)
        {
            var links = input.TafsiliLinks ?? Array.Empty<VoucherDetailTafsiliLinkInput>();
            await _tafsiliLevelGuard.EnsureSatisfiedAsync(input.AccountId, links, cancellationToken);

            var detail = new TB_ELAMDETAIL
            {
                ID = Guid.NewGuid(),
                ELAMHEAD_ID = head.ID,
                ACCOUNTCODE_ID = input.AccountId,
                DEBTOR = head.ELAMH_CASE == ElamCase.Creditor ? input.Amount : 0,
                CREDITOR = head.ELAMH_CASE == ElamCase.Debtor ? input.Amount : 0,
                ELAMD_DESC = Blank(input.Description),
                ELAM_ATRIBNO = Blank(input.AttribNo),
                VAHEDCODE = head.VAHEDCODE,
                YEAR = head.YEAR,
                ADDUSERID = userId,
                CREATEDDATE = now,
                ISDELETED = false,
            };
            await _repository.AddDetailAsync(detail, cancellationToken);
            await StageLinksAsync(detail, links.Select(l => (l.TafsiliId, l.LevelId)), now, userId, cancellationToken);
        }
    }

    private async Task StageLinksAsync(
        TB_ELAMDETAIL detail, IEnumerable<(Guid TafsiliId, Guid LevelId)> links, DateTime now, string userId,
        CancellationToken cancellationToken)
    {
        foreach (var (tafsiliId, levelId) in links)
        {
            await _repository.AddDetailLinkAsync(new TB_ELAMDETAIL_LINK_TAFSILI
            {
                ID = Guid.NewGuid(),
                ELAMDETAIL_ID = detail.ID,
                TAFSILI_ID = tafsiliId,
                LEVEL_ID = levelId,
                VAHEDCODE = detail.VAHEDCODE,
                YEAR = detail.YEAR,
                ADDUSERID = userId,
                CREATEDDATE = now,
                ISDELETED = false,
            }, cancellationToken);
        }
    }

    /// <summary>حذف نرم ردیف‌ها و تفصیلی‌های فعال (ویرایش = جایگزینی کامل؛ حذف = cascade).</summary>
    public void SoftDeleteDetails(TB_ELAMHEAD head)
    {
        var now = DateTime.UtcNow;
        foreach (var d in head.TB_ELAMDETAILs.Where(d => d.ISDELETED != true))
        {
            d.ISDELETED = true;
            d.UPDATEDDATE = now;
            d.CHANGEUSERID = _currentUser.UserId;
            foreach (var l in d.TB_ELAMDETAIL_LINK_TAFSILIs.Where(l => l.ISDELETED != true))
            {
                l.ISDELETED = true;
                l.UPDATEDDATE = now;
                l.CHANGEUSERID = _currentUser.UserId;
            }
        }
    }

    /// <summary>
    /// سند موقت اعلامیه را می‌سازد: ردیف‌ها عیناً + یک ردیف حساب رابط به جمع طرف مقابل.
    /// <c>VOUCHERSHEAD_ID</c> سرسند را پر می‌کند و شمارهٔ سند را برمی‌گرداند.
    /// </summary>
    public async Task<string> IssueVoucherAsync(
        TB_ELAMHEAD head, IReadOnlyList<TB_ELAMDETAIL> details, CancellationToken cancellationToken)
    {
        if (details.Count == 0)
            throw new ElamConflictException("اعلامیه ردیفی ندارد.");

        var kind = KindOf(head.WEB_STAT);
        var rabet = await GetRabetAsync(kind, cancellationToken);
        await EnsureRabetHasNoRequiredTafsiliAsync(rabet, cancellationToken);

        var vahedCode = head.VAHEDCODE!;
        var year = head.YEAR!;
        var counterName = head.ELAMH_SENDRCVVAHED is null
            ? null
            : await _repository.GetVahedNameAsync(head.ELAMH_SENDRCVVAHED, cancellationToken);
        var counter = counterName ?? head.ELAMH_SENDRCVVAHED ?? "—";
        var headDesc = kind switch
        {
            ElamKind.Received => $"اعلامیه رسیده از واحد {counter} به شماره سریال {head.ELAMH_SERIALNO}",
            ElamKind.Revenue => $"اعلامیه صادره درآمد به واحد {counter} به شماره سریال {head.ELAMH_SERIALNO}",
            _ => $"اعلامیه صادره به واحد {counter} به شماره سریال {head.ELAMH_SERIALNO}",
        };

        var docNum = await _voucherHeadRepository.GetNextDocNumAsync(vahedCode, year, cancellationToken);
        var now = DateTime.UtcNow;
        var userId = _currentUser.UserId;
        var voucher = new TB_VOUCHERSHEAD
        {
            ID = Guid.NewGuid(),
            DOC_NUM = docNum,
            DATE_DOC = head.ELAMH_DATE,
            DOCLIFE = DocLife.Temporary,
            HEAD_DESC = Truncate(headDesc, 250),
            VAHEDCODE = vahedCode,
            YEAR = year,
            ISAUTOMATIC = true,
            ADDUSERID = userId,
            CREATEDDATE = now,
            ISDELETED = false,
        };
        await _voucherHeadRepository.AddAsync(voucher, cancellationToken);

        var radif = 1;
        foreach (var d in details)
        {
            var line = await AddVoucherLineAsync(
                voucher, radif++, d.ACCOUNTCODE_ID, d.DEBTOR ?? 0, d.CREDITOR ?? 0, d.ELAMD_DESC, now, userId, cancellationToken);
            foreach (var l in d.TB_ELAMDETAIL_LINK_TAFSILIs.Where(l => l.ISDELETED != true && l.TAFSILI_ID != null && l.LEVEL_ID != null))
            {
                await _voucherDetailRepository.AddTafsiliLinkAsync(new TB_VOUCHERDETAIL_LINK_TAFSILI
                {
                    ID = Guid.NewGuid(),
                    VOUCHERSDETAIL_ID = line.ID,
                    TAFSILI_ID = l.TAFSILI_ID!.Value,
                    LEVEL_ID = l.LEVEL_ID!.Value,
                    VAHEDCODE = vahedCode,
                    YEAR = year,
                    ADDUSERID = userId,
                    CREATEDDATE = now,
                    ISDELETED = false,
                }, cancellationToken);
            }
            await StageAttributeAsync(line, d.ELAM_ATRIBNO, now, userId, cancellationToken);
        }

        // ردیف رابط: طرف مقابل جمع ردیف‌ها (مرجع: Creditor = ΣDebtor اگر ماهیت بستانکار، و برعکس).
        var rabetDebtor = head.ELAMH_CASE == ElamCase.Debtor ? details.Sum(d => d.CREDITOR ?? 0) : 0;
        var rabetCreditor = head.ELAMH_CASE == ElamCase.Creditor ? details.Sum(d => d.DEBTOR ?? 0) : 0;
        var rabetPrefix = kind == ElamKind.Received ? "رابط اعلامیه رسیده" : "رابط اعلامیه صادره";
        var rabetLine = await AddVoucherLineAsync(
            voucher, radif, rabet.AccountId, rabetDebtor, rabetCreditor,
            $"{rabetPrefix} - {head.ELAMH_SENDRCVVAHED}{head.ELAMH_SERIALNO}", now, userId, cancellationToken);
        await StageAttributeAsync(rabetLine, RabetAttributeValue(head), now, userId, cancellationToken);

        head.VOUCHERSHEAD_ID = voucher.ID;
        head.ELAMH_CODE ??= rabet.AccCode;
        head.UPDATEDDATE = now;
        head.CHANGEUSERID = userId;
        return docNum;
    }

    /// <summary>
    /// تأیید نهایی صادره: اعلامیهٔ رسیده در واحد مقصد (وضعیت ۹) با ردیف‌های عیناً کپی‌شده، و سند آن
    /// (وضعیت ۱۰). شناسهٔ اعلامیهٔ صادره در <c>ELAMSENDERID</c> رسیده می‌نشیند.
    /// </summary>
    public async Task<TB_ELAMHEAD> CreateReceivedAsync(
        TB_ELAMHEAD sender, IReadOnlyList<TB_ELAMDETAIL> senderDetails, CancellationToken cancellationToken)
    {
        var target = sender.ELAMH_SENDRCVVAHED
            ?? throw new ElamConflictException("واحد گیرندهٔ اعلامیه مشخص نیست.");
        var year = sender.YEAR!;
        var rabet = await GetRabetAsync(ElamKind.Received, cancellationToken);
        var serial = await _repository.GetNextSerialAsync(target, year, rabet.AccCode, cancellationToken);
        var senderName = await _repository.GetVahedNameAsync(sender.VAHEDCODE!, cancellationToken);
        var now = DateTime.UtcNow;
        var userId = _currentUser.UserId;

        var rcv = new TB_ELAMHEAD
        {
            ID = Guid.NewGuid(),
            ELAMH_SERIALNO = serial,
            ELAMH_CODE = rabet.AccCode,
            ELAMH_CASE = sender.ELAMH_CASE,
            ELAMH_DATE = sender.ELAMH_DATE,
            ELAMH_DESC = Truncate($"اعلامیه رسیده از واحد {senderName ?? sender.VAHEDCODE}", 300),
            ELAMH_DABIRNO = sender.ELAMH_DABIRNO,
            ELAMH_DABIRDATE = sender.ELAMH_DABIRDATE,
            ELAMH_SENDRCVVAHED = sender.VAHEDCODE,
            ELAMSENDERID = sender.ID,
            WEB_STAT = (byte)ElamWebStat.RcvInfo,
            VAHEDCODE = target,
            YEAR = year,
            ADDUSERID = userId,
            CREATEDDATE = now,
            ISDELETED = false,
        };
        await _repository.AddHeadAsync(rcv, cancellationToken);

        var copies = new List<TB_ELAMDETAIL>();
        foreach (var d in senderDetails)
        {
            var copy = new TB_ELAMDETAIL
            {
                ID = Guid.NewGuid(),
                ELAMHEAD_ID = rcv.ID,
                ACCOUNTCODE_ID = d.ACCOUNTCODE_ID,
                DEBTOR = d.DEBTOR,
                CREDITOR = d.CREDITOR,
                ELAMD_DESC = d.ELAMD_DESC,
                ELAM_ATRIBNO = d.ELAM_ATRIBNO,
                VAHEDCODE = target,
                YEAR = year,
                ADDUSERID = userId,
                CREATEDDATE = now,
                ISDELETED = false,
            };
            await _repository.AddDetailAsync(copy, cancellationToken);
            foreach (var l in d.TB_ELAMDETAIL_LINK_TAFSILIs.Where(l => l.ISDELETED != true && l.TAFSILI_ID != null && l.LEVEL_ID != null))
            {
                var link = new TB_ELAMDETAIL_LINK_TAFSILI
                {
                    ID = Guid.NewGuid(),
                    ELAMDETAIL_ID = copy.ID,
                    TAFSILI_ID = l.TAFSILI_ID,
                    LEVEL_ID = l.LEVEL_ID,
                    VAHEDCODE = target,
                    YEAR = year,
                    ADDUSERID = userId,
                    CREATEDDATE = now,
                    ISDELETED = false,
                };
                await _repository.AddDetailLinkAsync(link, cancellationToken);
                copy.TB_ELAMDETAIL_LINK_TAFSILIs.Add(link);
            }
            copies.Add(copy);
        }

        await IssueVoucherAsync(rcv, copies, cancellationToken);
        rcv.WEB_STAT = (byte)ElamWebStat.ConfirmRcvInfo;
        return rcv;
    }

    private static string? RabetAttributeValue(TB_ELAMHEAD head)
        => head.ELAMH_SERIALNO is { Length: >= 14 } serial
            ? $"{head.ELAMH_SENDRCVVAHED}3{serial.Substring(9, 5)}"
            : null;

    private async Task<TB_VOUCHERSDETAIL> AddVoucherLineAsync(
        TB_VOUCHERSHEAD voucher, int radif, Guid? accountId, decimal debtor, decimal creditor, string? description,
        DateTime now, string userId, CancellationToken cancellationToken)
    {
        var line = new TB_VOUCHERSDETAIL
        {
            ID = Guid.NewGuid(),
            VOUCHERSHEAD_ID = voucher.ID,
            ACCOUNT_ID = accountId,
            DESCRIPTION = description is null ? null : Truncate(description, 200),
            RADIF = radif,
            DEBTOR = debtor,
            CREDITOR = creditor,
            VAHEDCODE = voucher.VAHEDCODE,
            YEAR = voucher.YEAR,
            ADDUSERID = userId,
            CREATEDDATE = now,
            ISDELETED = false,
        };
        await _voucherDetailRepository.AddAsync(line, cancellationToken);
        return line;
    }

    /// <summary>شناسه فقط وقتی ثبت می‌شود که معین در واحد و سال سند «شناسه‌دار» تعریف شده باشد.</summary>
    private async Task StageAttributeAsync(
        TB_VOUCHERSDETAIL line, string? value, DateTime now, string userId, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(value) || line.ACCOUNT_ID is null)
            return;
        var definitionId = await _repository.GetAttribDefinitionIdAsync(
            line.ACCOUNT_ID.Value, line.VAHEDCODE!, line.YEAR!, cancellationToken);
        if (definitionId is null)
            return;
        await _repository.AddAttribInVoucherAsync(new TB_ATTRIBSINVOUCHER
        {
            ID = Guid.NewGuid(),
            VOUCHERSDETAIL_ID = line.ID,
            ATTRIBFORACCOUNTCODE_ID = definitionId.Value,
            ATTRIBUTEVALUE = value.Trim(),
            VAHEDCODE = line.VAHEDCODE!,
            YEAR = line.YEAR!,
            ADDUSERID = userId,
            CREATEDDATE = now,
            ISDELETED = false,
        }, cancellationToken);
    }

    private async Task EnsureRabetHasNoRequiredTafsiliAsync(ElamRabetAccount rabet, CancellationToken cancellationToken)
    {
        try
        {
            await _tafsiliLevelGuard.EnsureSatisfiedAsync(
                rabet.AccountId, Array.Empty<VoucherDetailTafsiliLinkInput>(), cancellationToken);
        }
        catch (TafsiliLevelRuleException)
        {
            throw new ElamConflictException(
                $"حساب رابط {rabet.AccCode} سطح تفصیلی الزامی دارد؛ حساب رابط اعلامیه نباید تفصیلی الزامی داشته باشد.");
        }
    }

    private static string? Blank(string? s) => string.IsNullOrWhiteSpace(s) ? null : s.Trim();

    private static string Truncate(string s, int max) => s.Length <= max ? s : s[..max];
}
