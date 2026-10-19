using System.Text.Json.Serialization;
using Accounting.Application.Common.Exceptions;
using Accounting.Application.Common.Interfaces;
using Accounting.Application.Common.Security;
using Accounting.Application.Commands.Vouchers.Common;
using Accounting.Domain.Entity;
using Accounting.Domain.ValueObjects;
using FluentValidation;
using MediatR;

namespace Accounting.Application.Vouchers.Inbox;

// «دریافت اسناد از سایر سیستم‌ها» (تصمیم صاحب پروژه ۲۰۲۶-۱۰-۰۷، ریسک‌های #۱۶ و #۲۷).
// سیستم‌هایی مثل حقوق سرسند/ردیف را در TB_TMP_VOUCHERHEAD/TB_TMP_VOUCHERSDETAIL می‌نویسند (کد معین
// و کد تفصیلی سطح ۱..۷)؛ کاربر واحد در این صفحه می‌بیند و با «دریافت» سند یادداشت می‌سازد. این تنها
// مسیر «سند سیستمی» است: معین «فقط سیستمی» این‌جا مجاز است و در فرم دستی نه.

public sealed record SystemVoucherInboxItemDto(
    Guid Id,
    string? SysType,
    string? DateDoc,
    string? Year,
    string? HeadDesc,
    DateTime? CreatedDate,
    int LineCount,
    decimal Debtor,
    decimal Creditor,
    Guid? VoucherHeadId,
    string? VoucherDocNum);

public sealed record SystemVoucherInboxLineDto(
    int? Radif,
    string? MoinCode,
    string? MoinName,
    IReadOnlyList<SystemVoucherInboxTafsiliDto> Tafsilis,
    string? Description,
    decimal Debtor,
    decimal Creditor,
    string? Error);

public sealed record SystemVoucherInboxTafsiliDto(int Level, string Code, string? Name);

public sealed record SystemVoucherInboxDetailDto(
    SystemVoucherInboxItemDto Head,
    IReadOnlyList<SystemVoucherInboxLineDto> Lines,
    bool CanReceive,
    IReadOnlyList<string> DocumentErrors);

public sealed record GetSystemVoucherInboxQuery(string? Year, bool IncludeReceived)
    : IRequest<IReadOnlyList<SystemVoucherInboxItemDto>>, IVahedScopedQuery
{
    [JsonIgnore]
    public string VahedCode { get; set; } = string.Empty;
}

public sealed record GetSystemVoucherInboxDetailQuery(Guid Id) : IRequest<SystemVoucherInboxDetailDto>, IVahedScopedQuery
{
    [JsonIgnore]
    public string VahedCode { get; set; } = string.Empty;
}

public sealed record ReceiveSystemVoucherCommand(Guid Id) : IRequest<ReceiveSystemVoucherResult>, IVahedScopedCommand
{
    [JsonIgnore]
    public string VahedCode { get; set; } = string.Empty;
}

public sealed record ReceiveSystemVoucherResult(Guid VoucherHeadId, string DocNum);

public sealed class ReceiveSystemVoucherCommandValidator : AbstractValidator<ReceiveSystemVoucherCommand>
{
    public ReceiveSystemVoucherCommandValidator() => RuleFor(x => x.Id).NotEqual(Guid.Empty);
}

/// <summary>تبدیل ردیف‌های موقت به حساب/تفصیلی واقعی — مشترک بین پیش‌نمایش و دریافت.</summary>
public sealed class SystemVoucherResolver
{
    private readonly ISystemVoucherInboxRepository _repo;

    public SystemVoucherResolver(ISystemVoucherInboxRepository repo) => _repo = repo;

    public sealed record Resolved(
        TB_TMP_VOUCHERSDETAIL Source,
        Guid? AccountId,
        string? MoinName,
        IReadOnlyList<(int Level, string Code, Guid? TafsiliId, Guid? LevelId, string? Name)> Tafsilis,
        string? Error,
        VoucherLineExtrasInput? Extras = null,
        Guid? SoriCheckBookId = null);

    public async Task<IReadOnlyList<Resolved>> ResolveAsync(IReadOnlyList<TB_TMP_VOUCHERSDETAIL> lines, string vahedCode, CancellationToken ct)
    {
        var moins = await _repo.ResolveMoinsAsync(
            lines.Select(l => l.MOINCODE?.Trim()).Where(c => !string.IsNullOrEmpty(c)).Select(c => c!).ToList(), ct);
        var tafsiliCodes = lines.SelectMany(TafsiliCodes).Select(t => t.Code).ToList();
        var tafsilis = await _repo.ResolveTafsilisAsync(tafsiliCodes, vahedCode, ct);
        var levels = await _repo.GetLevelIdsByCodeAsync(ct);

        return lines.Select(l =>
        {
            var errors = new List<string>();
            var code = l.MOINCODE?.Trim();
            (Guid Id, string Name) moin = default;
            var hasMoin = !string.IsNullOrEmpty(code) && moins.TryGetValue(code, out moin);
            if (string.IsNullOrEmpty(code))
                errors.Add("کد معین ندارد");
            else if (!hasMoin)
                errors.Add($"معین «{code}» پیدا نشد");

            var debtor = l.DEBTOR ?? 0;
            var creditor = l.CREDITOR ?? 0;
            if (debtor < 0 || creditor < 0 || decimal.Truncate(debtor) != debtor || decimal.Truncate(creditor) != creditor)
                errors.Add("مبلغ باید ریال صحیح و نامنفی باشد");
            if ((debtor > 0) == (creditor > 0))
                errors.Add("هر ردیف باید فقط بدهکار یا فقط بستانکار باشد");

            var taf = TafsiliCodes(l).Select(t =>
            {
                var found = tafsilis.TryGetValue(t.Code, out var x);
                var levelFound = levels.TryGetValue(t.Level, out var levelId);
                if (!found)
                    errors.Add($"تفصیلی «{t.Code}» (سطح {t.Level}) پیدا نشد");
                if (!levelFound)
                    errors.Add($"سطح تفصیلی {t.Level} تعریف نشده است");
                return (t.Level, t.Code, found ? x.Id : (Guid?)null, levelFound ? levelId : (Guid?)null, found ? x.Name : null);
            }).ToList();

            return new Resolved(l, hasMoin ? moin.Id : null, hasMoin ? moin.Name : null, taf,
                errors.Count > 0 ? string.Join("؛ ", errors) : null);
        }).ToList();
    }

    private static IEnumerable<(int Level, string Code)> TafsiliCodes(TB_TMP_VOUCHERSDETAIL l)
    {
        string?[] codes = [l.TAFSILI_CODE1, l.TAFSILI_CODE2, l.TAFSILI_CODE3, l.TAFSILI_CODE4, l.TAFSILI_CODE5, l.TAFSILI_CODE6, l.TAFSILI_CODE7];
        for (var i = 0; i < codes.Length; i++)
        {
            var c = codes[i]?.Trim();
            if (!string.IsNullOrEmpty(c))
                yield return (i + 1, c);
        }
    }
}

/// <summary>
/// همهٔ کنترل‌های «دریافت» — مشترک بین پیش‌نمایش و دریافت تا آنچه کاربر می‌بیند همان چیزی باشد که ثبت می‌شود.
/// سطح ردیف: معین تعریف‌شده، تفصیلی‌ها تعریف‌شده و قابل‌دید برای واحد، ریال صحیح، فقط بدهکار یا بستانکار،
/// ماتریس دسترسی کدینگ واحد (حالت سیستمی)، تفصیلی الزامی/غیرمجاز برای معین، تفصیلی مجاز برای (معین، سطح، واحد).
/// سطح سند: ردیف دارد، تاریخ/سال معتبر و هم‌خوان، تراز (جمع بدهکار = جمع بستانکار و بزرگ‌تر از صفر)،
/// ردیف‌ها همان واحد سرسندند، و همان سند (SOURCEID) قبلاً برای این واحد دریافت نشده است.
/// </summary>
public sealed class SystemVoucherChecker
{
    private readonly ISystemVoucherInboxRepository _repo;
    private readonly IVoucherTafsiliLevelGuard _tafsiliLevelGuard;
    private readonly IAccountEntryPolicy _accountEntryPolicy;
    private readonly IVoucherLineExtrasStore _extrasStore;
    private readonly IVoucherLineExtrasService _extrasService;
    private readonly IChequeBookRepository _chequeBooks;

    public SystemVoucherChecker(
        ISystemVoucherInboxRepository repo,
        IVoucherTafsiliLevelGuard tafsiliLevelGuard,
        IAccountEntryPolicy accountEntryPolicy,
        IVoucherLineExtrasStore extrasStore,
        IVoucherLineExtrasService extrasService,
        IChequeBookRepository chequeBooks)
    {
        _repo = repo;
        _tafsiliLevelGuard = tafsiliLevelGuard;
        _accountEntryPolicy = accountEntryPolicy;
        _extrasStore = extrasStore;
        _extrasService = extrasService;
        _chequeBooks = chequeBooks;
    }

    /// <summary>
    /// حساب بانکی/شناسه‌دار/ویژگی‌دار (تصمیم صاحب پروژه ۲۰۲۶-۱۰-۱۲): ردیف بستانکار حساب بانکی ⇒ چک صوری (اعلامیه) از
    /// دسته‌چک صوری همان حساب بانکی صادر می‌شود؛ حساب شناسه‌دار ⇒ مقدار شناسه باید در ستون VALUE آمده باشد (چند شناسه با
    /// «|» به ترتیب شمارهٔ کادر)؛ حساب یا تفصیلی ویژگی‌دار از سایر سیستم‌ها نمی‌آید ⇒ رد.
    /// </summary>
    private async Task<(VoucherLineExtrasInput? Extras, Guid? SoriBookId, List<string> Errors)> CheckExtrasAsync(
        SystemVoucherResolver.Resolved r, Guid accountId, string vahedCode, string year, CancellationToken ct)
    {
        var errors = new List<string>();
        var tafsiliIds = r.Tafsilis.Where(t => t.TafsiliId is not null).Select(t => t.TafsiliId!.Value).Distinct().ToList();
        var requirements = await _extrasStore.GetRequirementsAsync(accountId, tafsiliIds, vahedCode, year, ct);

        if (requirements.Identities.Count > 0)
        {
            errors.Add("حساب یا تفصیلی این ردیف ویژگی (شناسنامه) دارد و سند ویژگی‌دار از سایر سیستم‌ها دریافت نمی‌شود");
            return (null, null, errors);
        }

        var defs = requirements.Attributes.OrderBy(a => a.BoxNo).ToList();
        var parts = (r.Source.VALUE ?? string.Empty).Split('|', StringSplitOptions.TrimEntries);
        if (defs.Count == 0 && !string.IsNullOrWhiteSpace(r.Source.VALUE))
            errors.Add("برای این حساب شناسه تعریف نشده ولی مقدار شناسه ارسال شده است");
        else if (defs.Count > 0 && parts.Length > defs.Count)
            errors.Add($"تعداد مقدارهای شناسه ({parts.Length}) از شناسه‌های تعریف‌شدهٔ حساب ({defs.Count}) بیشتر است");

        var attributes = defs
            .Select((d, i) => new VoucherLineAttributeInput(d.DefinitionId, i < parts.Length ? parts[i] : null))
            .ToList();
        var extras = new VoucherLineExtrasInput(attributes, null, null, FromOtherSystem: true);

        var isDebit = (r.Source.DEBTOR ?? 0) > 0;
        errors.AddRange(await _extrasService.ValidateAsync(accountId, tafsiliIds, isDebit, extras, vahedCode, year, ct));

        Guid? soriBookId = null;
        if (requirements.IsBankAccount && !isDebit)
        {
            var book = (await _chequeBooks.GetSoriBooksAsync(vahedCode, accountId, ct)).FirstOrDefault(b => b.NextNumber is not null);
            if (book is null)
                errors.Add("حساب بانکی است و برای صدور چک صوری (اعلامیه) دسته‌چک صوری با برگ آزاد برایش تعریف نشده است");
            else
                soriBookId = book.CheckBookId;
        }

        return (extras, soriBookId, errors);
    }

    public sealed record Outcome(
        IReadOnlyList<SystemVoucherResolver.Resolved> Lines,
        IReadOnlyList<string> DocumentErrors,
        string? DateDoc,
        string? Year)
    {
        public bool IsValid => DocumentErrors.Count == 0 && Lines.Count > 0 && Lines.All(l => l.Error is null);
    }

    public async Task<Outcome> CheckAsync(
        TB_TMP_VOUCHERHEAD head, IReadOnlyList<TB_TMP_VOUCHERSDETAIL> lines, string vahedCode, CancellationToken ct)
    {
        var docErrors = new List<string>();

        if (head.VOUCHERSHEAD_ID is not null)
            docErrors.Add("این سند قبلاً دریافت شده است.");
        if (lines.Count == 0)
            docErrors.Add("این سند ردیفی ندارد.");

        var dateDoc = head.DATE_DOC?.Trim();
        string? year = null;
        if (dateDoc is null || dateDoc.Length != 8 || !dateDoc.All(char.IsDigit))
        {
            docErrors.Add("تاریخ سند ارسالی نامعتبر است (باید ۸ رقم yyyyMMdd باشد).");
        }
        else
        {
            year = string.IsNullOrWhiteSpace(head.YEAR) ? dateDoc[..4] : head.YEAR.Trim();
            if (!dateDoc.StartsWith(year, StringComparison.Ordinal))
                docErrors.Add("تاریخ سند ارسالی با سال مالی آن نمی‌خواند.");
        }

        var totalDebtor = lines.Sum(l => l.DEBTOR ?? 0);
        var totalCreditor = lines.Sum(l => l.CREDITOR ?? 0);
        if (lines.Count > 0 && (totalDebtor != totalCreditor || totalDebtor <= 0))
            docErrors.Add($"سند تراز نیست یا مبلغ ندارد (جمع بدهکار {totalDebtor:N0} / جمع بستانکار {totalCreditor:N0}).");

        if (lines.Any(l => !string.IsNullOrWhiteSpace(l.VAHEDCODE) && l.VAHEDCODE != vahedCode))
            docErrors.Add("ردیف‌هایی از واحد دیگری در این سند آمده است.");

        if (head.SOURCEID is { } sourceId && head.VOUCHERSHEAD_ID is null
            && await _repo.IsSourceAlreadyReceivedAsync(sourceId, vahedCode, head.ID, ct))
            docErrors.Add("سندی با همین شناسهٔ منبع (SOURCEID) قبلاً برای این واحد دریافت شده است؛ ارسال تکراری است.");

        var resolved = await new SystemVoucherResolver(_repo).ResolveAsync(lines, vahedCode, ct);
        var checkedLines = new List<SystemVoucherResolver.Resolved>(resolved.Count);
        var policyCache = new Dictionary<Guid, string?>();
        foreach (var r in resolved)
        {
            var errors = new List<string>();
            if (r.Error is not null)
                errors.Add(r.Error);

            if (r.AccountId is { } accountId)
            {
                if (!policyCache.TryGetValue(accountId, out var policyError))
                {
                    try
                    {
                        await _accountEntryPolicy.EnsureSystemEntryAllowedAsync(vahedCode, dateDoc, [accountId], ct);
                        policyError = null;
                    }
                    catch (BusinessRuleException ex)
                    {
                        policyError = ex.Message;
                    }

                    policyCache[accountId] = policyError;
                }

                if (policyError is not null)
                    errors.Add(policyError);

                if (r.Tafsilis.All(t => t.TafsiliId is not null && t.LevelId is not null))
                {
                    var links = r.Tafsilis
                        .DistinctBy(t => (t.TafsiliId, t.LevelId))
                        .Select(t => new VoucherDetailTafsiliLinkInput(t.TafsiliId!.Value, t.LevelId!.Value))
                        .ToList();
                    try
                    {
                        await _tafsiliLevelGuard.EnsureSatisfiedAsync(accountId, links, ct);
                        await _tafsiliLevelGuard.EnsureTafsiliSelectableAsync(accountId, links, vahedCode, cancellationToken: ct);
                    }
                    catch (TafsiliLevelRuleException ex)
                    {
                        errors.Add(ex.PublicDetail);
                    }
                    catch (BusinessRuleException ex)
                    {
                        errors.Add(ex.Message);
                    }
                }
            }

            VoucherLineExtrasInput? extras = null;
            Guid? soriBookId = null;
            if (r.AccountId is { } extrasAccountId && year is not null)
            {
                var (e, book, extrasErrors) = await CheckExtrasAsync(r, extrasAccountId, vahedCode, year, ct);
                extras = e;
                soriBookId = book;
                errors.AddRange(extrasErrors);
            }

            checkedLines.Add(r with
            {
                Error = errors.Count > 0 ? string.Join("؛ ", errors.Distinct()) : null,
                Extras = extras,
                SoriCheckBookId = soriBookId,
            });
        }

        return new Outcome(checkedLines, docErrors, dateDoc, year);
    }
}

internal static class SystemVoucherInboxMapping
{
    public static SystemVoucherInboxItemDto ToDto(SystemVoucherInboxRow r) => new(
        r.Id, r.SysType, r.DateDoc, r.Year, r.HeadDesc, r.CreatedDate, r.LineCount, r.Debtor, r.Creditor, r.VoucherHeadId, r.VoucherDocNum);
}

public sealed class GetSystemVoucherInboxQueryHandler : IRequestHandler<GetSystemVoucherInboxQuery, IReadOnlyList<SystemVoucherInboxItemDto>>
{
    private readonly ISystemVoucherInboxRepository _repo;

    public GetSystemVoucherInboxQueryHandler(ISystemVoucherInboxRepository repo) => _repo = repo;

    public async Task<IReadOnlyList<SystemVoucherInboxItemDto>> Handle(GetSystemVoucherInboxQuery q, CancellationToken ct)
        => (await _repo.ListAsync(q.VahedCode, q.Year, q.IncludeReceived, ct)).Select(SystemVoucherInboxMapping.ToDto).ToList();
}

public sealed class GetSystemVoucherInboxDetailQueryHandler : IRequestHandler<GetSystemVoucherInboxDetailQuery, SystemVoucherInboxDetailDto>
{
    private readonly ISystemVoucherInboxRepository _repo;
    private readonly SystemVoucherChecker _checker;

    public GetSystemVoucherInboxDetailQueryHandler(ISystemVoucherInboxRepository repo, SystemVoucherChecker checker)
    {
        _repo = repo;
        _checker = checker;
    }

    public async Task<SystemVoucherInboxDetailDto> Handle(GetSystemVoucherInboxDetailQuery q, CancellationToken ct)
    {
        var found = await _repo.GetForReceiveAsync(q.Id, q.VahedCode, ct)
            ?? throw new NotFoundException(nameof(TB_TMP_VOUCHERHEAD), q.Id);
        var (head, lines) = found;
        var outcome = await _checker.CheckAsync(head, lines, q.VahedCode, ct);
        var resolved = outcome.Lines;

        var headDto = new SystemVoucherInboxItemDto(
            head.ID, head.SYS_TYPE, head.DATE_DOC, head.YEAR, head.HEAD_DESC, head.CREATEDDATE, lines.Count,
            lines.Sum(l => l.DEBTOR ?? 0), lines.Sum(l => l.CREDITOR ?? 0), head.VOUCHERSHEAD_ID, null);

        var lineDtos = resolved.Select(r => new SystemVoucherInboxLineDto(
            r.Source.RADIF, r.Source.MOINCODE, r.MoinName,
            r.Tafsilis.Select(t => new SystemVoucherInboxTafsiliDto(t.Level, t.Code, t.Name)).ToList(),
            r.Source.DETAIL_DESC, r.Source.DEBTOR ?? 0, r.Source.CREDITOR ?? 0, r.Error)).ToList();

        return new SystemVoucherInboxDetailDto(headDto, lineDtos, head.VOUCHERSHEAD_ID is null && outcome.IsValid, outcome.DocumentErrors);
    }
}

/// <summary>
/// «دریافت»: سند یادداشت با شمارهٔ بعدی واحد، <c>ISAUTOMATIC</c>، و <c>VOUCHERSHEAD_ID</c> سرسند موقت
/// پر می‌شود تا دوباره دریافت نشود. همه یا هیچ (یک <c>SaveChanges</c>). کنترل‌ها: معین/تفصیلی پیدا شوند،
/// تفصیلی الزامی، ماتریس دسترسی کدینگ در حالت سیستمی، ریال صحیح.
/// </summary>
public sealed class ReceiveSystemVoucherCommandHandler : IRequestHandler<ReceiveSystemVoucherCommand, ReceiveSystemVoucherResult>
{
    private readonly ISystemVoucherInboxRepository _repo;
    private readonly IVoucherHeadRepository _headRepository;
    private readonly IVoucherDetailRepository _detailRepository;
    private readonly SystemVoucherChecker _checker;
    private readonly IVoucherChequeService _chequeService;
    private readonly IVoucherLineExtrasService _extrasService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUser _currentUser;

    public ReceiveSystemVoucherCommandHandler(
        ISystemVoucherInboxRepository repo,
        IVoucherHeadRepository headRepository,
        IVoucherDetailRepository detailRepository,
        SystemVoucherChecker checker,
        IVoucherChequeService chequeService,
        IVoucherLineExtrasService extrasService,
        IUnitOfWork unitOfWork,
        ICurrentUser currentUser)
    {
        _repo = repo;
        _headRepository = headRepository;
        _detailRepository = detailRepository;
        _checker = checker;
        _chequeService = chequeService;
        _extrasService = extrasService;
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
    }

    public async Task<ReceiveSystemVoucherResult> Handle(ReceiveSystemVoucherCommand c, CancellationToken ct)
    {
        var found = await _repo.GetForReceiveAsync(c.Id, c.VahedCode, ct)
            ?? throw new NotFoundException(nameof(TB_TMP_VOUCHERHEAD), c.Id);
        var (tmp, lines) = found;

        var outcome = await _checker.CheckAsync(tmp, lines, c.VahedCode, ct);
        if (!outcome.IsValid)
        {
            var problems = outcome.DocumentErrors
                .Concat(outcome.Lines.Where(r => r.Error is not null).Take(3).Select(r => $"ردیف {r.Source.RADIF}: {r.Error}"))
                .ToList();
            throw new BusinessRuleException("سند قابل دریافت نیست — " + string.Join(" | ", problems));
        }

        var dateDoc = outcome.DateDoc!;
        var year = outcome.Year!;
        var resolved = outcome.Lines;

        var now = DateTime.UtcNow;
        var userId = _currentUser.UserId;
        var docNum = await _headRepository.GetNextDocNumAsync(c.VahedCode, year, ct);
        // نوع سیستم مبدأ (کد TB_SYSTYPE) → SYSTEM_TYPE سند؛ کد ناشناخته (سندهای قدیمی) = بدون نوع.
        var sysType = string.IsNullOrWhiteSpace(tmp.SYS_TYPE) ? null : await _repo.ResolveSysTypeAsync(tmp.SYS_TYPE, ct);

        var head = new TB_VOUCHERSHEAD
        {
            ID = Guid.NewGuid(),
            DOC_NUM = docNum,
            DATE_DOC = dateDoc,
            DOCLIFE = DocLife.Draft,
            HEAD_DESC = Truncate(tmp.HEAD_DESC, 250),
            APENDIX = Truncate($"دریافت از سیستم {sysType?.Name?.Trim() ?? tmp.SYS_TYPE}".Trim(), 800),
            SYSTEM_TYPE = sysType?.Id,
            VAHEDCODE = c.VahedCode,
            YEAR = year,
            ISAUTOMATIC = true,
            ADDUSERID = userId,
            CREATEDDATE = now,
            ISDELETED = false,
        };
        // تراکنش: صدور چک صوری شمارهٔ بعدی را از دیتابیس می‌خواند، پس بعد از هر چک ذخیره می‌کنیم؛ خطا همه را برمی‌گرداند.
        await _unitOfWork.BeginTransactionAsync(ct);
        try
        {
        await _headRepository.AddAsync(head, ct);

        var radif = 0;
        foreach (var r in resolved)
        {
            var detail = new TB_VOUCHERSDETAIL
            {
                ID = Guid.NewGuid(),
                VOUCHERSHEAD_ID = head.ID,
                ACCOUNT_ID = r.AccountId,
                DESCRIPTION = Truncate(r.Source.DETAIL_DESC, 200),
                RADIF = ++radif,
                DEBTOR = r.Source.DEBTOR is > 0 ? r.Source.DEBTOR : null,
                CREDITOR = r.Source.CREDITOR is > 0 ? r.Source.CREDITOR : null,
                VAHEDCODE = c.VahedCode,
                YEAR = year,
                ADDUSERID = userId,
                CREATEDDATE = now,
                ISDELETED = false,
            };
            await _detailRepository.AddAsync(detail, ct);

            foreach (var t in r.Tafsilis.DistinctBy(t => (t.TafsiliId, t.LevelId)))
            {
                await _detailRepository.AddTafsiliLinkAsync(new TB_VOUCHERDETAIL_LINK_TAFSILI
                {
                    ID = Guid.NewGuid(),
                    VOUCHERSDETAIL_ID = detail.ID,
                    TAFSILI_ID = t.TafsiliId!.Value,
                    LEVEL_ID = t.LevelId!.Value,
                    VAHEDCODE = c.VahedCode,
                    YEAR = year,
                    ADDUSERID = userId,
                    CREATEDDATE = now,
                    ISDELETED = false,
                }, ct);
            }

            // حساب بانکی (برداشت) ⇒ چک صوری از دسته‌چک صوری همان حساب؛ تاریخ چک = CHECK_DATE فرستنده یا تاریخ سند.
            if (r.SoriCheckBookId is { } bookId)
            {
                var checkDate = r.Source.CHECK_DATE?.Trim();
                if (checkDate is not { Length: 8 } || !checkDate.All(char.IsDigit))
                    checkDate = dateDoc;
                detail.CHECK_ID = await _chequeService.ApplyAsync(
                    null, detail.ID, true,
                    new VoucherChequeInfoInput(null, checkDate, Truncate(r.Source.DETAIL_DESC, 800), bookId),
                    c.VahedCode, ct);
                await _unitOfWork.SaveChangesAsync(ct);
            }

            // شناسه‌ها (VALUE) — همان قاعدهٔ فرم سند.
            await _extrasService.ApplyAsync(
                detail,
                r.Tafsilis.Where(t => t.TafsiliId is not null).Select(t => t.TafsiliId!.Value).Distinct().ToList(),
                r.Extras, replace: false, ct);
        }

        tmp.VOUCHERSHEAD_ID = head.ID;
        tmp.UPDATEDDATE = now;
        tmp.CHANGEUSERID = userId;

        await _unitOfWork.SaveChangesAsync(ct);
        await _unitOfWork.CommitTransactionAsync(ct);
        }
        catch
        {
            await _unitOfWork.RollbackTransactionAsync(ct);
            throw;
        }

        return new ReceiveSystemVoucherResult(head.ID, docNum);
    }

    private static string? Truncate(string? value, int max)
        => string.IsNullOrWhiteSpace(value) ? null : value.Length <= max ? value : value[..max];
}
