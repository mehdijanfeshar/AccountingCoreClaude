using Accounting.Application.Common.Interfaces;
using Accounting.Domain.Entity;

namespace Accounting.Application.Vouchers.Inbox;

// ورودی صندوق «دریافت اسناد از سایر سیستم‌ها» از کافکا (۲۰۲۶-۱۰-۱۷). قالب پیام همان پروژهٔ قدیم است
// (VoucherMessageDto). پیام فقط در TB_TMP_VOUCHERHEAD/SDETAIL می‌نشیند؛ کنترل‌های کسب‌وکاری (معین،
// تفصیلی، تراز) همان پیش‌نمایش صندوق است و کاربر واحد با «دریافت» سند یادداشت می‌سازد.
// عمداً MediatR نیست: در پس‌زمینه کاربر/هدر واحد نداریم و رفتارهای نقش و واحد معنا ندارند؛ واحد از
// خود پیام می‌آید و این‌جا وجودش کنترل می‌شود. لاگ تغییرات با کاربر «سیستم» ثبت می‌شود.

public sealed class SystemVoucherMessage
{
    public string? DateDoc { get; set; }
    public string? HeadDesc { get; set; }
    /// <summary>نوع سیستم مبدأ = <c>TB_SYSTYPE.SYS_COD</c> (الزامی) → <c>SYS_TYPE</c> (یک کاراکتر).</summary>
    public string? SysType { get; set; }
    /// <summary>نام قدیمی <see cref="SysType"/> (قالب پروژهٔ قدیم)؛ فقط وقتی SysType خالی است.</summary>
    public string? DocType { get; set; }

    public string? EffectiveSysType => string.IsNullOrWhiteSpace(SysType) ? DocType : SysType;
    public string? VahedCode { get; set; }
    public string? Year { get; set; }
    /// <summary>شناسهٔ سند در سیستم مبدأ؛ پیام تکراری با همین شناسه و واحد دوباره ثبت نمی‌شود.</summary>
    public Guid? SourceId { get; set; }
    public List<SystemVoucherMessageLine> Details { get; set; } = [];
}

public sealed class SystemVoucherMessageLine
{
    public string? MoinCode { get; set; }
    public string? TafsiliCode1 { get; set; }
    public string? TafsiliCode2 { get; set; }
    public string? TafsiliCode3 { get; set; }
    public string? TafsiliCode4 { get; set; }
    public string? TafsiliCode5 { get; set; }
    public string? TafsiliCode6 { get; set; }
    public string? TafsiliCode7 { get; set; }
    public string? Description { get; set; }
    public int? Radif { get; set; }
    public decimal? Debtor { get; set; }
    public decimal? Creditor { get; set; }
    public string? AttribValue { get; set; }
    public string? CheckDate { get; set; }
}

public enum SystemVoucherIngestStatus
{
    Created,
    Duplicate,
    Rejected,
}

public sealed record SystemVoucherIngestResult(SystemVoucherIngestStatus Status, Guid? TmpVoucherId, IReadOnlyList<string> Errors)
{
    public static SystemVoucherIngestResult Reject(IReadOnlyList<string> errors) => new(SystemVoucherIngestStatus.Rejected, null, errors);
}

public interface ISystemVoucherIngestService
{
    Task<SystemVoucherIngestResult> IngestAsync(SystemVoucherMessage message, CancellationToken ct);
}

public sealed class SystemVoucherIngestService : ISystemVoucherIngestService
{
    private readonly ISystemVoucherInboxRepository _repository;
    private readonly IUnitOfWork _unitOfWork;

    public SystemVoucherIngestService(ISystemVoucherInboxRepository repository, IUnitOfWork unitOfWork)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public async Task<SystemVoucherIngestResult> IngestAsync(SystemVoucherMessage message, CancellationToken ct)
    {
        var errors = Validate(message);
        if (errors.Count > 0)
            return SystemVoucherIngestResult.Reject(errors);

        var vahedCode = message.VahedCode!.Trim();
        if (!await _repository.VahedExistsAsync(vahedCode, ct))
            return SystemVoucherIngestResult.Reject([$"واحد «{vahedCode}» تعریف نشده است."]);

        var sysType = message.EffectiveSysType!.Trim();
        if (await _repository.ResolveSysTypeAsync(sysType, ct) is null)
            return SystemVoucherIngestResult.Reject([$"نوع سیستم «{sysType}» در «انواع سیستم» (TB_SYSTYPE) تعریف نشده است."]);

        if (message.SourceId is { } sourceId
            && await _repository.FindStagedBySourceAsync(sourceId, vahedCode, ct) is { } existingId)
            return new SystemVoucherIngestResult(SystemVoucherIngestStatus.Duplicate, existingId, []);

        var now = DateTime.UtcNow;
        var year = message.Year!.Trim();
        var head = new TB_TMP_VOUCHERHEAD
        {
            ID = Guid.NewGuid(),
            DATE_DOC = Clean(message.DateDoc),
            HEAD_DESC = Clean(message.HeadDesc),
            VAHEDCODE = vahedCode,
            YEAR = year,
            SYS_TYPE = sysType,
            SOURCEID = message.SourceId,
            ADDUSERID = SystemUserId,
            CREATEDDATE = now,
            ISDELETED = false,
        };

        var lines = message.Details.Select((d, i) => new TB_TMP_VOUCHERSDETAIL
        {
            ID = Guid.NewGuid(),
            TMPVOUCHERHEAD_ID = head.ID,
            MOINCODE = Clean(d.MoinCode),
            TAFSILI_CODE1 = Clean(d.TafsiliCode1),
            TAFSILI_CODE2 = Clean(d.TafsiliCode2),
            TAFSILI_CODE3 = Clean(d.TafsiliCode3),
            TAFSILI_CODE4 = Clean(d.TafsiliCode4),
            TAFSILI_CODE5 = Clean(d.TafsiliCode5),
            TAFSILI_CODE6 = Clean(d.TafsiliCode6),
            TAFSILI_CODE7 = Clean(d.TafsiliCode7),
            RADIF = d.Radif ?? i + 1,
            DETAIL_DESC = Clean(d.Description),
            DEBTOR = d.Debtor ?? 0,
            CREDITOR = d.Creditor ?? 0,
            CHECK_DATE = Clean(d.CheckDate),
            VALUE = Clean(d.AttribValue),
            VAHEDCODE = vahedCode,
            YEAR = year,
            ADDUSERID = SystemUserId,
            CREATEDDATE = now,
            ISDELETED = false,
        }).ToList();

        await _repository.StageAsync(head, lines, ct);
        await _unitOfWork.SaveChangesAsync(ct);

        return new SystemVoucherIngestResult(SystemVoucherIngestStatus.Created, head.ID, []);
    }

    /// <summary>ثبت‌کنندهٔ ردیف‌های آمده از کافکا (ستون ADDUSERID حداکثر ۱۰ کاراکتر).</summary>
    public const string SystemUserId = "KAFKA";

    // فقط کنترل ساختاری و طول ستون‌ها (تا ذخیره با ORA-12899 نشکند)؛ قواعد کسب‌وکاری در پیش‌نمایش صندوق.
    private static List<string> Validate(SystemVoucherMessage m)
    {
        var errors = new List<string>();
        if (string.IsNullOrWhiteSpace(m.VahedCode))
            errors.Add("کد واحد خالی است.");
        else
            MaxLength(errors, "کد واحد", m.VahedCode, 4);

        if (string.IsNullOrWhiteSpace(m.Year) || m.Year.Trim().Length != 4)
            errors.Add("سال مالی باید ۴ رقم باشد.");

        MaxLength(errors, "تاریخ سند", m.DateDoc, 8);
        MaxLength(errors, "شرح سند", m.HeadDesc, 250);
        // TB_SYSTYPE.SYS_COD تا ۴ کاراکتر است ولی ستون SYS_TYPE سند موقت فقط ۱ کاراکتر.
        if (string.IsNullOrWhiteSpace(m.EffectiveSysType))
            errors.Add("نوع سیستم (SysType) خالی است.");
        else
            MaxLength(errors, "نوع سیستم (SysType)", m.EffectiveSysType, 1);

        if (m.Details is not { Count: > 0 })
        {
            errors.Add("سند ردیف ندارد.");
            return errors;
        }

        for (var i = 0; i < m.Details.Count; i++)
        {
            var d = m.Details[i];
            var at = $"ردیف {d.Radif ?? i + 1}: ";
            if (string.IsNullOrWhiteSpace(d.MoinCode))
                errors.Add(at + "کد معین خالی است.");
            MaxLength(errors, at + "کد معین", d.MoinCode, 6);
            foreach (var (code, level) in new[] { d.TafsiliCode1, d.TafsiliCode2, d.TafsiliCode3, d.TafsiliCode4, d.TafsiliCode5, d.TafsiliCode6, d.TafsiliCode7 }.Select((c, n) => (c, n + 1)))
                MaxLength(errors, at + $"کد تفصیلی سطح {level}", code, 11);
            MaxLength(errors, at + "شرح", d.Description, 200);
            MaxLength(errors, at + "تاریخ چک", d.CheckDate, 8);
            MaxLength(errors, at + "مقدار ویژگی", d.AttribValue, 20);
            if (d.Debtor < 0 || d.Creditor < 0)
                errors.Add(at + "مبلغ منفی مجاز نیست.");
            // مبلغ خالی = صفر (نه «اعشاری»): null % 1 != 0 در C# درست است و ردیف یک‌طرفه را رد می‌کرد.
            if ((d.Debtor ?? 0) % 1 != 0 || (d.Creditor ?? 0) % 1 != 0)
                errors.Add(at + "مبلغ باید ریال صحیح باشد.");
        }

        return errors;
    }

    private static void MaxLength(List<string> errors, string field, string? value, int max)
    {
        if (value is not null && value.Trim().Length > max)
            errors.Add($"{field} بیش از {max} کاراکتر است.");
    }

    private static string? Clean(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
