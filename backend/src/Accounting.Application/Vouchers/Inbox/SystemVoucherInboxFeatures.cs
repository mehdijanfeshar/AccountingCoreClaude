using System.Text.Json.Serialization;
using Accounting.Application.Common.Exceptions;
using Accounting.Application.Common.Interfaces;
using Accounting.Application.Common.Security;
using Accounting.Application.Vouchers.Commands.Common;
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

public sealed record SystemVoucherInboxDetailDto(SystemVoucherInboxItemDto Head, IReadOnlyList<SystemVoucherInboxLineDto> Lines, bool CanReceive);

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
internal sealed class SystemVoucherResolver
{
    private readonly ISystemVoucherInboxRepository _repo;

    public SystemVoucherResolver(ISystemVoucherInboxRepository repo) => _repo = repo;

    public sealed record Resolved(
        TB_TMP_VOUCHERSDETAIL Source,
        Guid? AccountId,
        string? MoinName,
        IReadOnlyList<(int Level, string Code, Guid? TafsiliId, Guid? LevelId, string? Name)> Tafsilis,
        string? Error);

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

    public GetSystemVoucherInboxDetailQueryHandler(ISystemVoucherInboxRepository repo) => _repo = repo;

    public async Task<SystemVoucherInboxDetailDto> Handle(GetSystemVoucherInboxDetailQuery q, CancellationToken ct)
    {
        var found = await _repo.GetForReceiveAsync(q.Id, q.VahedCode, ct)
            ?? throw new NotFoundException(nameof(TB_TMP_VOUCHERHEAD), q.Id);
        var (head, lines) = found;
        var resolved = await new SystemVoucherResolver(_repo).ResolveAsync(lines, q.VahedCode, ct);

        var headDto = new SystemVoucherInboxItemDto(
            head.ID, head.SYS_TYPE, head.DATE_DOC, head.YEAR, head.HEAD_DESC, head.CREATEDDATE, lines.Count,
            lines.Sum(l => l.DEBTOR ?? 0), lines.Sum(l => l.CREDITOR ?? 0), head.VOUCHERSHEAD_ID, null);

        var lineDtos = resolved.Select(r => new SystemVoucherInboxLineDto(
            r.Source.RADIF, r.Source.MOINCODE, r.MoinName,
            r.Tafsilis.Select(t => new SystemVoucherInboxTafsiliDto(t.Level, t.Code, t.Name)).ToList(),
            r.Source.DETAIL_DESC, r.Source.DEBTOR ?? 0, r.Source.CREDITOR ?? 0, r.Error)).ToList();

        return new SystemVoucherInboxDetailDto(headDto, lineDtos,
            head.VOUCHERSHEAD_ID is null && lines.Count > 0 && resolved.All(r => r.Error is null));
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
    private readonly IVoucherTafsiliLevelGuard _tafsiliLevelGuard;
    private readonly IAccountEntryPolicy _accountEntryPolicy;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUser _currentUser;

    public ReceiveSystemVoucherCommandHandler(
        ISystemVoucherInboxRepository repo,
        IVoucherHeadRepository headRepository,
        IVoucherDetailRepository detailRepository,
        IVoucherTafsiliLevelGuard tafsiliLevelGuard,
        IAccountEntryPolicy accountEntryPolicy,
        IUnitOfWork unitOfWork,
        ICurrentUser currentUser)
    {
        _repo = repo;
        _headRepository = headRepository;
        _detailRepository = detailRepository;
        _tafsiliLevelGuard = tafsiliLevelGuard;
        _accountEntryPolicy = accountEntryPolicy;
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
    }

    public async Task<ReceiveSystemVoucherResult> Handle(ReceiveSystemVoucherCommand c, CancellationToken ct)
    {
        var found = await _repo.GetForReceiveAsync(c.Id, c.VahedCode, ct)
            ?? throw new NotFoundException(nameof(TB_TMP_VOUCHERHEAD), c.Id);
        var (tmp, lines) = found;

        if (tmp.VOUCHERSHEAD_ID is not null)
            throw new BusinessRuleException("این سند قبلاً دریافت شده است.");
        if (lines.Count == 0)
            throw new BusinessRuleException("این سند ردیفی ندارد.");

        var dateDoc = tmp.DATE_DOC?.Trim();
        if (dateDoc is null || dateDoc.Length != 8 || !dateDoc.All(char.IsDigit))
            throw new BusinessRuleException("تاریخ سند ارسالی نامعتبر است (باید ۸ رقم yyyyMMdd باشد).");
        var year = string.IsNullOrWhiteSpace(tmp.YEAR) ? dateDoc[..4] : tmp.YEAR.Trim();
        if (!dateDoc.StartsWith(year, StringComparison.Ordinal))
            throw new BusinessRuleException("تاریخ سند ارسالی با سال مالی آن نمی‌خواند.");

        var resolved = await new SystemVoucherResolver(_repo).ResolveAsync(lines, c.VahedCode, ct);
        var bad = resolved.Where(r => r.Error is not null).Take(3).Select(r => $"ردیف {r.Source.RADIF}: {r.Error}").ToList();
        if (bad.Count > 0)
            throw new BusinessRuleException("سند قابل دریافت نیست — " + string.Join(" | ", bad));

        foreach (var r in resolved)
        {
            await _tafsiliLevelGuard.EnsureSatisfiedAsync(
                r.AccountId,
                r.Tafsilis.Select(t => new VoucherDetailTafsiliLinkInput(t.TafsiliId!.Value, t.LevelId!.Value)).ToList(),
                ct);
        }

        await _accountEntryPolicy.EnsureSystemEntryAllowedAsync(c.VahedCode, dateDoc, resolved.Select(r => r.AccountId), ct);

        var now = DateTime.UtcNow;
        var userId = _currentUser.UserId;
        var docNum = await _headRepository.GetNextDocNumAsync(c.VahedCode, year, ct);

        var head = new TB_VOUCHERSHEAD
        {
            ID = Guid.NewGuid(),
            DOC_NUM = docNum,
            DATE_DOC = dateDoc,
            DOCLIFE = DocLife.Draft,
            HEAD_DESC = Truncate(tmp.HEAD_DESC, 250),
            APENDIX = Truncate($"دریافت از سیستم {tmp.SYS_TYPE}".Trim(), 800),
            VAHEDCODE = c.VahedCode,
            YEAR = year,
            ISAUTOMATIC = true,
            ADDUSERID = userId,
            CREATEDDATE = now,
            ISDELETED = false,
        };
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
        }

        tmp.VOUCHERSHEAD_ID = head.ID;
        tmp.UPDATEDDATE = now;
        tmp.CHANGEUSERID = userId;

        await _unitOfWork.SaveChangesAsync(ct);
        return new ReceiveSystemVoucherResult(head.ID, docNum);
    }

    private static string? Truncate(string? value, int max)
        => string.IsNullOrWhiteSpace(value) ? null : value.Length <= max ? value : value[..max];
}
