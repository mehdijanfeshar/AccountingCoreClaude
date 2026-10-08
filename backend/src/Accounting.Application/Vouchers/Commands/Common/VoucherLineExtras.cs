using System.Globalization;
using System.Text.RegularExpressions;
using Accounting.Application.Common.Interfaces;
using Accounting.Domain.Entity;
using Accounting.Domain.ValueObjects;

namespace Accounting.Application.Vouchers.Commands.Common;

// ───────────── ورودی ─────────────

/// <summary>مقدار یک شناسهٔ حساب شناسه‌دار (<c>TB_ATTRIBSINVOUCHER</c>).</summary>
public sealed record VoucherLineAttributeInput(Guid DefinitionId, string? Value);

/// <summary>مقدار یک فیلد متغیر ویژگی (<c>TB_IDENTITYDETAIL</c>).</summary>
public sealed record VoucherLineIdentityValueInput(Guid SubGroupId, string? Value);

/// <summary>ویژگی (شناسنامه) ردیف: کدام شناسنامهٔ گروه، و مقدار فیلدهای متغیرش.</summary>
public sealed record VoucherLineIdentityInput(Guid GroupId, Guid? HeadId, IReadOnlyList<VoucherLineIdentityValueInput>? Values);

/// <summary>فیش/حوالهٔ واریز به بانک — هنگام ثبت ردیف در <c>TB_RECEIP</c> ساخته (یا اصلاح) می‌شود.</summary>
public sealed record VoucherLineReceiptInput(ReceiptType Kind, string? No, string? Date);

/// <summary>
/// اطلاعات تکمیلی ردیف سند: شناسه‌های حساب شناسه‌دار، ویژگی‌های تفصیلی/حساب، و فیش بانک.
/// null روی Update یعنی «دست نزن» (مثل <c>TafsiliLinks</c>)؛ روی Create یعنی «چیزی وارد نشده» و
/// اگر حساب شناسه یا ویژگی بخواهد خطا می‌دهد.
/// </summary>
public sealed record VoucherLineExtrasInput(
    IReadOnlyList<VoucherLineAttributeInput>? Attributes = null,
    IReadOnlyList<VoucherLineIdentityInput>? Identities = null,
    VoucherLineReceiptInput? Receipt = null);

// ───────────── نیازمندی‌ها (برای UI و اعتبارسنجی) ─────────────

public sealed record AttributeRequirement(
    Guid DefinitionId,
    int BoxNo,
    AttribFlag Flag,
    int Length,
    AttribControl? Control,
    string AccountCode,
    string AccountTitle);

public sealed record IdentityFieldInfo(
    Guid SubGroupId,
    string Title,
    IdentitySubGroupKind Kind,
    IdentitySubGroupType? Type,
    int Length);

public sealed record IdentityRequirement(
    Guid GroupId,
    string Title,
    Guid? TafsiliId,
    IReadOnlyList<IdentityFieldInfo> Fields);

public sealed record VoucherLineRequirements(
    IReadOnlyList<AttributeRequirement> Attributes,
    IReadOnlyList<IdentityRequirement> Identities,
    bool IsBankAccount)
{
    public bool IsEmpty => Attributes.Count == 0 && Identities.Count == 0 && !IsBankAccount;
}

public sealed record IdentityHeadFixedValue(Guid SubGroupId, string Title, string? Value);

public sealed record IdentityHeadOption(Guid HeadId, int Serial, IReadOnlyList<IdentityHeadFixedValue> FixedValues);

/// <summary>مقادیر ذخیره‌شدهٔ یک ردیف (برای حالت ویرایش).</summary>
public sealed record VoucherLineExtrasDto(
    IReadOnlyList<VoucherLineAttributeInput> Attributes,
    IReadOnlyList<VoucherLineIdentityInput> Identities,
    Guid? ReceiptId,
    VoucherLineReceiptInput? Receipt);

/// <summary>خواندن تعریف‌ها و stage کردن مقادیر — پیاده‌سازی در Infrastructure.</summary>
public interface IVoucherLineExtrasStore
{
    /// <summary>
    /// شناسه‌های معین (یا اگر معین تعریف ندارد، حساب کل بالای آن) در واحد و سال؛ ویژگی‌های وصل به
    /// خود حساب (<c>TB_ACCOUNTCODE.IDENTYGROUPS_ID</c>) یا به تفصیلی‌های ردیف (<c>TB_IDENTITYGROUP.TAFSILI_ID</c>)؛
    /// و بانکی بودن معین (حساب بانکی <c>TB_ACCOUNT</c> روی آن).
    /// </summary>
    Task<VoucherLineRequirements> GetRequirementsAsync(
        Guid accountId, IReadOnlyCollection<Guid> tafsiliIds, string vahedCode, string year, CancellationToken ct);

    Task<IReadOnlyList<IdentityHeadOption>> ListHeadsAsync(Guid groupId, string vahedCode, string year, CancellationToken ct);

    Task<bool> HeadBelongsToGroupAsync(Guid headId, Guid groupId, string vahedCode, CancellationToken ct);

    Task<bool> ReceiptNoExistsAsync(ReceiptType kind, string no, string vahedCode, string year, Guid? exceptId, CancellationToken ct);

    Task<VoucherLineExtrasDto> GetSavedAsync(Guid detailId, string vahedCode, CancellationToken ct);

    /// <summary>شناسه‌ها و ویژگی‌های فعال ردیف را حذف نرم می‌کند (پیش از نوشتن مقادیر تازه).</summary>
    Task SoftDeleteExtrasAsync(Guid detailId, string userId, DateTime now, CancellationToken ct);

    Task<TB_RECEIP?> GetReceiptForUpdateAsync(Guid receiptId, string vahedCode, CancellationToken ct);

    Task AddAttributeAsync(TB_ATTRIBSINVOUCHER row, CancellationToken ct);
    Task AddIdentityDetailAsync(TB_IDENTITYDETAIL row, CancellationToken ct);
    Task AddReceiptAsync(TB_RECEIP row, CancellationToken ct);
}

/// <summary>اطلاعات تکمیلی ردیف ناقص یا نادرست است — ۴۲۲ با پیام فارسی.</summary>
public sealed class VoucherLineExtrasException : Exception
{
    public VoucherLineExtrasException(IReadOnlyList<string> messages)
        : base(string.Join(" ", messages))
    {
        Messages = messages;
    }

    public IReadOnlyList<string> Messages { get; }

    public string PublicDetail => string.Join(" ", Messages);
}

// ───────────── سرویس ─────────────

/// <summary>
/// شناسه، ویژگی و فیش ردیف سند — یک قاعده برای همهٔ مسیرهای ثبت (فرم سند، ویرایش ردیف، حسابیار).
/// <list type="bullet">
/// <item>حساب شناسه‌دار: برای هر شناسهٔ تعریف‌شده (در «تعریف حساب‌های شناسه‌دار» روی معین یا کل) مقدار الزامی است
/// و باید با نوع (عدد/تاریخ)، طول و کنترل (غیرصفر/تاریخ) همان تعریف بخواند.</item>
/// <item>ویژگی: اگر حساب یا یکی از تفصیلی‌های ردیف به گروه ویژگی وصل است، شناسنامهٔ همان گروه باید انتخاب شود
/// و همهٔ فیلدهای <b>متغیر</b> آن مقدار بگیرند (فیلدهای ثابت از خود شناسنامه می‌آیند).</item>
/// <item>فیش: فقط روی ردیف بدهکار حساب بانکی و آنجا <b>الزامی</b>؛ شماره در همان نوع/واحد/سال تکراری نباشد.
/// ردیف بستانکار حساب بانکی بدون برگ چک یا چک صوری ثبت نمی‌شود.</item>
/// </list>
/// </summary>
public interface IVoucherLineExtrasService
{
    /// <summary>بدون نوشتن؛ پیام‌های خطا (خالی = درست).</summary>
    Task<IReadOnlyList<string>> ValidateAsync(
        Guid accountId, IReadOnlyCollection<Guid> tafsiliIds, bool isDebit, VoucherLineExtrasInput? extras,
        string vahedCode, string year, CancellationToken ct);

    /// <summary>
    /// اعتبارسنجی و stage روی ردیف (بدون SaveChanges). <paramref name="replace"/> = ردیف موجود؛
    /// مقادیر قبلی حذف نرم و تازه‌ها نوشته می‌شوند. ورودی null روی ردیف موجود = دست نزن.
    /// </summary>
    Task ApplyAsync(
        TB_VOUCHERSDETAIL line, IReadOnlyCollection<Guid> tafsiliIds, VoucherLineExtrasInput? extras, bool replace,
        CancellationToken ct);
}

public sealed class VoucherLineExtrasService : IVoucherLineExtrasService
{
    private readonly IVoucherLineExtrasStore _store;
    private readonly ICurrentUser _currentUser;

    public VoucherLineExtrasService(IVoucherLineExtrasStore store, ICurrentUser currentUser)
    {
        _store = store;
        _currentUser = currentUser;
    }

    public async Task<IReadOnlyList<string>> ValidateAsync(
        Guid accountId, IReadOnlyCollection<Guid> tafsiliIds, bool isDebit, VoucherLineExtrasInput? extras,
        string vahedCode, string year, CancellationToken ct)
    {
        var requirements = await _store.GetRequirementsAsync(accountId, tafsiliIds, vahedCode, year, ct);
        return await CheckAsync(requirements, isDebit, extras, vahedCode, year, null, ct);
    }

    public async Task ApplyAsync(
        TB_VOUCHERSDETAIL line, IReadOnlyCollection<Guid> tafsiliIds, VoucherLineExtrasInput? extras, bool replace,
        CancellationToken ct)
    {
        if (replace && extras is null)
            return;
        if (line.ACCOUNT_ID is not { } accountId || string.IsNullOrEmpty(line.VAHEDCODE) || string.IsNullOrEmpty(line.YEAR))
            return;

        var vahed = line.VAHEDCODE;
        var year = line.YEAR;
        var requirements = await _store.GetRequirementsAsync(accountId, tafsiliIds, vahed, year, ct);
        var isDebit = (line.DEBTOR ?? 0) > 0;
        var isCredit = (line.CREDITOR ?? 0) > 0;
        var errors = await CheckAsync(requirements, isDebit, extras, vahed, year, replace ? line.RECEIP_ID : null, ct);
        // CHECK_ID is already set by VoucherChequeService (real leaf, or the leaf issued for a fictitious cheque).
        if (requirements.IsBankAccount && isCredit && line.CHECK_ID is null)
            errors.Add(BankCreditNeedsCheque);
        if (errors.Count > 0)
            throw new VoucherLineExtrasException(errors);

        var now = DateTime.UtcNow;
        var userId = _currentUser.UserId;

        if (replace)
            await _store.SoftDeleteExtrasAsync(line.ID, userId, now, ct);

        foreach (var def in requirements.Attributes)
        {
            var value = extras!.Attributes!.First(a => a.DefinitionId == def.DefinitionId).Value!;
            await _store.AddAttributeAsync(new TB_ATTRIBSINVOUCHER
            {
                ID = Guid.NewGuid(),
                VOUCHERSDETAIL_ID = line.ID,
                ATTRIBFORACCOUNTCODE_ID = def.DefinitionId,
                ATTRIBUTEVALUE = NormalizeAttribute(def, value),
                VAHEDCODE = vahed,
                YEAR = year,
                ADDUSERID = userId,
                CREATEDDATE = now,
                ISDELETED = false,
            }, ct);
        }

        foreach (var group in requirements.Identities)
        {
            var input = extras!.Identities!.First(i => i.GroupId == group.GroupId);
            foreach (var field in group.Fields.Where(f => f.Kind == IdentitySubGroupKind.Variable))
            {
                var value = input.Values!.First(v => v.SubGroupId == field.SubGroupId).Value!;
                await _store.AddIdentityDetailAsync(new TB_IDENTITYDETAIL
                {
                    ID = Guid.NewGuid(),
                    IDENTITYSUBGRPS_ID = field.SubGroupId,
                    IDENTITYHEAD_ID = input.HeadId!.Value,
                    VOUCHERSDETAIL_ID = line.ID,
                    DETAIL_VALUE = NormalizeIdentity(field, value),
                    VAHEDCODE = vahed,
                    YEAR = year,
                    ADDUSERID = userId,
                    CREATEDDATE = now,
                    ISDELETED = false,
                }, ct);
            }
        }

        if (extras?.Receipt is { } receipt && requirements.IsBankAccount && isDebit)
        {
            var no = Digits(receipt.No!.Trim());
            var date = Digits(receipt.Date!.Trim());
            var existing = replace && line.RECEIP_ID is { } rid ? await _store.GetReceiptForUpdateAsync(rid, vahed, ct) : null;
            if (existing is not null)
            {
                existing.RECEIPT_KIND = receipt.Kind;
                existing.RECEIPT_NO = no;
                existing.RECEIPT_DATE = date;
                existing.CHANGEUSERID = userId;
                existing.UPDATEDDATE = now;
            }
            else
            {
                var row = new TB_RECEIP
                {
                    ID = Guid.NewGuid(),
                    RECEIPT_KIND = receipt.Kind,
                    RECEIPT_NO = no,
                    RECEIPT_DATE = date,
                    VAHEDCODE = vahed,
                    YEAR = year,
                    ADDUSERID = userId,
                    CREATEDDATE = now,
                    ISDELETED = false,
                };
                await _store.AddReceiptAsync(row, ct);
                line.RECEIP_ID = row.ID;
            }
        }
    }

    /// <summary>
    /// ردیف حساب بانکی (معینی که در تعریف حساب بانک انتخاب شده) بدون مدرک ثبت نمی‌شود — تصمیم صاحب پروژه ۲۰۲۶-۱۰-۰۸:
    /// بدهکار (واریز) ⇒ فیش/حواله با شمارهٔ دستی؛ بستانکار (برداشت) ⇒ برگ چک واقعی یا چک صوری (اعلامیه).
    /// </summary>
    internal const string BankDebitNeedsReceipt = "واریز به حساب بانکی بدون فیش/حواله ثبت نمی‌شود؛ شمارهٔ فیش یا حواله را وارد کنید.";
    internal const string BankCreditNeedsCheque = "برداشت از حساب بانکی بدون چک ثبت نمی‌شود؛ برگ چک یا چک صوری (اعلامیه) را انتخاب کنید.";

    private async Task<List<string>> CheckAsync(
        VoucherLineRequirements requirements, bool isDebit, VoucherLineExtrasInput? extras, string vahed, string year,
        Guid? currentReceiptId, CancellationToken ct)
    {
        var errors = new List<string>();

        foreach (var def in requirements.Attributes)
        {
            var label = requirements.Attributes.Count > 1 ? $"شناسهٔ {def.BoxNo} حساب {def.AccountCode}" : $"شناسهٔ حساب {def.AccountCode}";
            var value = extras?.Attributes?.FirstOrDefault(a => a.DefinitionId == def.DefinitionId)?.Value;
            if (string.IsNullOrWhiteSpace(value))
            {
                errors.Add($"{label} الزامی است.");
                continue;
            }
            if (AttributeError(def, value) is { } problem)
                errors.Add($"{label}: {problem}");
        }

        foreach (var group in requirements.Identities)
        {
            var input = extras?.Identities?.FirstOrDefault(i => i.GroupId == group.GroupId);
            if (input?.HeadId is not { } headId)
            {
                errors.Add($"شناسنامهٔ ویژگی «{group.Title}» انتخاب نشده است.");
                continue;
            }
            if (!await _store.HeadBelongsToGroupAsync(headId, group.GroupId, vahed, ct))
            {
                errors.Add($"شناسنامهٔ انتخاب‌شده متعلق به ویژگی «{group.Title}» نیست.");
                continue;
            }
            foreach (var field in group.Fields.Where(f => f.Kind == IdentitySubGroupKind.Variable))
            {
                var value = input.Values?.FirstOrDefault(v => v.SubGroupId == field.SubGroupId)?.Value;
                if (string.IsNullOrWhiteSpace(value))
                    errors.Add($"«{field.Title}» (ویژگی {group.Title}) الزامی است.");
                else if (IdentityError(field, value) is { } problem)
                    errors.Add($"«{field.Title}» (ویژگی {group.Title}): {problem}");
            }
        }

        if (requirements.IsBankAccount && isDebit && extras?.Receipt is null)
            errors.Add(BankDebitNeedsReceipt);

        if (extras?.Receipt is { } receipt)
        {
            if (!requirements.IsBankAccount)
                errors.Add("فیش/حواله فقط برای ردیف حساب بانکی است.");
            else if (!isDebit)
                errors.Add("فیش/حواله فقط برای ردیف بدهکار (واریز به بانک) است؛ برای برداشت از بانک چک انتخاب کنید.");
            else
            {
                var no = Digits(receipt.No?.Trim() ?? "");
                var date = Digits(receipt.Date?.Trim() ?? "");
                if (!Enum.IsDefined(receipt.Kind))
                    errors.Add("نوع فیش/حواله نامعتبر است.");
                if (no.Length == 0 || no.Length > 8 || !no.All(char.IsAsciiDigit))
                    errors.Add("شمارهٔ فیش/حواله باید عدد و حداکثر ۸ رقم باشد.");
                else if (await _store.ReceiptNoExistsAsync(receipt.Kind, no, vahed, year, currentReceiptId, ct))
                    errors.Add($"شماره فیش /حواله {no} تکراری می باشد.");
                if (!IsJalali(date))
                    errors.Add("تاریخ فیش/حواله نامعتبر است.");
            }
        }

        return errors;
    }

    // ───── قالب شناسه ─────

    private static string? AttributeError(AttributeRequirement def, string raw)
    {
        var value = Digits(raw.Trim());
        if (def.Flag == AttribFlag.Date || def.Control == AttribControl.IsDate)
        {
            var d = value.Replace("/", "").Replace("-", "");
            return IsJalali(d) ? null : "باید تاریخ معتبر (مثلاً ۱۴۰۴/۰۷/۰۸) باشد.";
        }
        if (!value.All(char.IsAsciiDigit))
            return "فقط عدد مجاز است.";
        if (def.Length > 0 && value.Length > def.Length)
            return $"حداکثر {def.Length} رقم است.";
        if (def.Control == AttribControl.NotZero && value.Trim('0').Length == 0)
            return "نباید صفر باشد.";
        return null;
    }

    private static string NormalizeAttribute(AttributeRequirement def, string raw)
    {
        var value = Digits(raw.Trim());
        return def.Flag == AttribFlag.Date || def.Control == AttribControl.IsDate
            ? value.Replace("/", "").Replace("-", "")
            : value;
    }

    // ───── قالب فیلد ویژگی ─────

    private static readonly Regex PersianText = new(@"^[؀-ۿ‌\s0-9۰-۹\-_.()/]+$", RegexOptions.Compiled);
    private static readonly Regex LatinText = new(@"^[A-Za-z\s0-9\-_.()/]+$", RegexOptions.Compiled);

    private static string? IdentityError(IdentityFieldInfo field, string raw)
    {
        var value = raw.Trim();
        if (field.Length > 0 && value.Length > field.Length)
            return $"حداکثر {field.Length} کاراکتر است.";
        return field.Type switch
        {
            IdentitySubGroupType.Date => IsJalali(Digits(value).Replace("/", "").Replace("-", "")) ? null : "باید تاریخ معتبر باشد.",
            IdentitySubGroupType.Number => Digits(value).All(char.IsAsciiDigit) ? null : "فقط عدد مجاز است.",
            IdentitySubGroupType.PersianLetter => PersianText.IsMatch(value) ? null : "فقط حروف فارسی مجاز است.",
            IdentitySubGroupType.LatinLetter => LatinText.IsMatch(value) ? null : "فقط حروف لاتین مجاز است.",
            _ => null,
        };
    }

    private static string NormalizeIdentity(IdentityFieldInfo field, string raw)
    {
        var value = raw.Trim();
        return field.Type switch
        {
            IdentitySubGroupType.Date => Digits(value).Replace("/", "").Replace("-", ""),
            IdentitySubGroupType.Number => Digits(value),
            _ => value,
        };
    }

    private static bool IsJalali(string yyyymmdd)
    {
        if (yyyymmdd.Length != 8 || !yyyymmdd.All(char.IsAsciiDigit))
            return false;
        var y = int.Parse(yyyymmdd[..4], CultureInfo.InvariantCulture);
        var m = int.Parse(yyyymmdd[4..6], CultureInfo.InvariantCulture);
        var d = int.Parse(yyyymmdd[6..], CultureInfo.InvariantCulture);
        if (y < 1300 || y > 1499 || m < 1 || m > 12 || d < 1)
            return false;
        return d <= new PersianCalendar().GetDaysInMonth(y, m);
    }

    /// <summary>ارقام فارسی/عربی → لاتین.</summary>
    private static string Digits(string s)
    {
        var chars = s.ToCharArray();
        for (var i = 0; i < chars.Length; i++)
        {
            var c = chars[i];
            if (c >= '۰' && c <= '۹') chars[i] = (char)('0' + (c - '۰'));
            else if (c >= '٠' && c <= '٩') chars[i] = (char)('0' + (c - '٠'));
        }
        return new string(chars);
    }
}
