using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using Accounting.Application.Common.Exceptions;
using Accounting.Application.Common.Interfaces;
using Accounting.Application.Common.Security;
using Accounting.Application.OperationTemplates;
using Accounting.Domain.Sales;
using MediatR;

namespace Accounting.Application.Sales;

// ═══════════════ ماژول فروش — تنظیمات، نگاشت حساب، نرخ ارزش افزوده، نقش‌ها، کالا، مشتری ═══════════════

public sealed record SalesWriteResult(bool Success, Guid? Id, IReadOnlyList<string> Errors)
{
    public static SalesWriteResult Ok(Guid id) => new(true, id, []);
    public static SalesWriteResult Fail(IEnumerable<string> errors) => new(false, null, errors.ToList());
}

public sealed record SalesAccountMapDto(string RoleKey, Guid AccountId, string? AccountCode, string? AccountTitle, Guid? TafsiliId, string? TafsiliTitle);

public sealed record SalesSettingDto(
    decimal DiscountApprovalPercent, CreditPolicy CreditPolicy, bool SegregationOfDuties, bool BillBeforeDelivery,
    bool AllowNegativeStock, Guid? SystemTypeId, string? SellerId, string? MemoryId);

/// <param name="Configured">تنظیمات و حساب دریافتنی/فروش برای واحد تعریف شده است.</param>
public sealed record SalesSetupDto(
    SalesSettingDto Settings, IReadOnlyList<SalesAccountMapDto> Accounts, IReadOnlyList<SalesRole> MyRoles,
    bool IsAdmin, bool CanManage, bool Configured);

internal static class SalesDefaults
{
    public static SalesSettingDto ToDto(SalesSetting? s) => s is null
        ? new(10, CreditPolicy.Approve, true, false, false, null, null, null)
        : new(s.DiscountApprovalPercent, s.CreditPolicy, s.SegregationOfDuties, s.BillBeforeDelivery, s.AllowNegativeStock,
              s.SystemTypeId, s.SellerId, s.MemoryId);

    public static string? Clean(string? s, int max) => string.IsNullOrWhiteSpace(s) ? null : s.Trim()[..Math.Min(s.Trim().Length, max)];
}

// ─────────── تنظیمات واحد ───────────

public sealed record GetSalesSetupQuery : IRequest<SalesSetupDto>, IVahedScopedQuery
{
    [JsonIgnore] public string VahedCode { get; set; } = string.Empty;
}

public sealed class GetSalesSetupHandler : IRequestHandler<GetSalesSetupQuery, SalesSetupDto>
{
    private readonly ISalesRepository _repo;
    private readonly SalesAccess _access;
    private readonly ISubsidiaryAccountReader _accounts;
    private readonly IDetailReader _details;

    public GetSalesSetupHandler(ISalesRepository repo, SalesAccess access, ISubsidiaryAccountReader accounts, IDetailReader details)
    { _repo = repo; _access = access; _accounts = accounts; _details = details; }

    public async Task<SalesSetupDto> Handle(GetSalesSetupQuery q, CancellationToken ct)
    {
        var access = await _access.GetAsync(q.VahedCode, ct);
        var setting = await _repo.GetSettingAsync(q.VahedCode, false, ct);
        var maps = await _repo.GetAccountMapsAsync(q.VahedCode, false, ct);
        var accounts = await _accounts.GetByIdsAsync(maps.Select(m => m.AccountId).Distinct().ToList(), ct);
        var details = await _details.GetByIdsAsync(maps.Where(m => m.TafsiliId is not null).Select(m => m.TafsiliId!.Value).Distinct().ToList(), q.VahedCode, ct);
        var dto = maps.Select(m => new SalesAccountMapDto(m.RoleKey, m.AccountId,
            accounts.GetValueOrDefault(m.AccountId)?.Code, accounts.GetValueOrDefault(m.AccountId)?.Title,
            m.TafsiliId, m.TafsiliId is { } t ? details.GetValueOrDefault(t)?.Title : null)).ToList();
        var configured = setting is not null && maps.Any(m => m.RoleKey == SalesAccountRoles.Receivable);
        return new SalesSetupDto(SalesDefaults.ToDto(setting), dto, access.Roles.ToList(), access.IsAdmin, access.CanManage, configured);
    }
}

public sealed record SalesAccountMapInput(string RoleKey, Guid AccountId, Guid? TafsiliId);

public sealed record SaveSalesSetupCommand(SalesSettingDto Settings, IReadOnlyList<SalesAccountMapInput>? Accounts)
    : IRequest<SalesWriteResult>, IVahedScopedCommand
{
    [JsonIgnore] public string VahedCode { get; set; } = string.Empty;
}

public sealed class SaveSalesSetupHandler : IRequestHandler<SaveSalesSetupCommand, SalesWriteResult>
{
    private readonly ISalesRepository _repo;
    private readonly SalesAccess _access;
    private readonly ISubsidiaryAccountReader _accounts;
    private readonly IDetailReader _details;
    private readonly IUnitOfWork _uow;
    private readonly ICurrentUser _user;

    public SaveSalesSetupHandler(ISalesRepository repo, SalesAccess access, ISubsidiaryAccountReader accounts, IDetailReader details, IUnitOfWork uow, ICurrentUser user)
    { _repo = repo; _access = access; _accounts = accounts; _details = details; _uow = uow; _user = user; }

    public async Task<SalesWriteResult> Handle(SaveSalesSetupCommand c, CancellationToken ct)
    {
        await _access.RequireManageAsync(c.VahedCode, ct);
        var s = c.Settings;
        var errors = new List<string>();
        if (s.DiscountApprovalPercent is < 0 or > 100) errors.Add("آستانهٔ تخفیف باید بین ۰ و ۱۰۰ باشد.");
        if (!Enum.IsDefined(s.CreditPolicy)) errors.Add("سیاست سقف اعتبار نامعتبر است.");

        var inputs = (c.Accounts ?? []).Where(a => a.AccountId != Guid.Empty).ToList();
        if (inputs.Any(a => !SalesAccountRoles.All.Contains(a.RoleKey))) errors.Add("نقش حسابداری نامعتبر است.");
        if (inputs.GroupBy(a => a.RoleKey).Any(g => g.Count() > 1)) errors.Add("هر نقش حسابداری فقط یک حساب دارد.");
        var accounts = await _accounts.GetByIdsAsync(inputs.Select(a => a.AccountId).Distinct().ToList(), ct);
        foreach (var a in inputs.Where(a => !accounts.TryGetValue(a.AccountId, out var info) || !info.IsActive))
            errors.Add($"حساب نقش «{a.RoleKey}» معین فعال نیست.");
        var fixedIds = inputs.Where(a => a.TafsiliId is not null).Select(a => a.TafsiliId!.Value).Distinct().ToList();
        if (fixedIds.Count > 0)
        {
            var details = await _details.GetByIdsAsync(fixedIds, c.VahedCode, ct);
            foreach (var id in fixedIds.Where(id => !details.TryGetValue(id, out var d) || !d.IsActive || !d.IsVisibleToUnit))
                errors.Add("تفصیلی ثابت انتخاب‌شده برای این واحد فعال یا قابل استفاده نیست.");
        }
        if (errors.Count > 0) return SalesWriteResult.Fail(errors);

        var setting = await _repo.GetSettingAsync(c.VahedCode, true, ct);
        if (setting is null)
        {
            setting = new SalesSetting { VahedCode = c.VahedCode, CreatedBy = _user.UserId, CreatedAtUtc = DateTime.UtcNow };
            await _repo.AddSettingAsync(setting, ct);
        }
        setting.DiscountApprovalPercent = s.DiscountApprovalPercent;
        setting.CreditPolicy = s.CreditPolicy;
        setting.SegregationOfDuties = s.SegregationOfDuties;
        setting.BillBeforeDelivery = s.BillBeforeDelivery;
        setting.AllowNegativeStock = s.AllowNegativeStock;
        setting.SystemTypeId = s.SystemTypeId;
        setting.SellerId = SalesDefaults.Clean(s.SellerId, 20);
        setting.MemoryId = SalesDefaults.Clean(s.MemoryId, 20)?.ToUpperInvariant();

        if (c.Accounts is not null)
        {
            _repo.RemoveAccountMaps(await _repo.GetAccountMapsAsync(c.VahedCode, true, ct));
            foreach (var a in inputs)
                await _repo.AddAccountMapAsync(new SalesAccountMap { Id = Guid.NewGuid(), VahedCode = c.VahedCode, RoleKey = a.RoleKey, AccountId = a.AccountId, TafsiliId = a.TafsiliId }, ct);
        }
        await _uow.SaveChangesAsync(ct);
        return new(true, null, []);
    }
}

// ─────────── نرخ ارزش افزوده (سراسری، مدیر ستاد) ───────────

public sealed record SalesVatRateDto(Guid Id, string FromDate, decimal Rate, string? Note);

public sealed record ListSalesVatRatesQuery : IRequest<IReadOnlyList<SalesVatRateDto>>;

public sealed class ListSalesVatRatesHandler : IRequestHandler<ListSalesVatRatesQuery, IReadOnlyList<SalesVatRateDto>>
{
    private readonly ISalesRepository _repo;
    public ListSalesVatRatesHandler(ISalesRepository repo) => _repo = repo;

    public async Task<IReadOnlyList<SalesVatRateDto>> Handle(ListSalesVatRatesQuery q, CancellationToken ct) =>
        (await _repo.ListVatRatesAsync(false, ct)).Where(r => !r.IsDeleted).OrderBy(r => r.FromDate)
            .Select(r => new SalesVatRateDto(r.Id, r.FromDate, r.Rate, r.Note)).ToList();
}

public sealed record SalesVatRateInput(string FromDate, decimal Rate, string? Note);

/// <summary>جایگزینی کل جدول نرخ (ردیف‌های قبلی حذف نرم). فقط مدیر ستاد.</summary>
public sealed record SaveSalesVatRatesCommand(IReadOnlyList<SalesVatRateInput> Rates) : IRequest<SalesWriteResult>;

public sealed class SaveSalesVatRatesHandler : IRequestHandler<SaveSalesVatRatesCommand, SalesWriteResult>
{
    private readonly ISalesRepository _repo;
    private readonly IUnitOfWork _uow;
    private readonly ICurrentUser _user;

    public SaveSalesVatRatesHandler(ISalesRepository repo, IUnitOfWork uow, ICurrentUser user) { _repo = repo; _uow = uow; _user = user; }

    public async Task<SalesWriteResult> Handle(SaveSalesVatRatesCommand c, CancellationToken ct)
    {
        if (!_user.IsInRole(AppRoles.SetadAdmin))
            throw new RoleAccessDeniedException("نرخ مالیات بر ارزش افزوده سراسری است و فقط مدیر ستاد آن را تغییر می‌دهد.");
        var errors = new List<string>();
        if (c.Rates.Count == 0) errors.Add("حداقل یک نرخ لازم است.");
        foreach (var r in c.Rates)
        {
            if (!SalesDates.IsValid(r.FromDate)) errors.Add($"تاریخ «{r.FromDate}» معتبر نیست (yyyyMMdd شمسی).");
            if (r.Rate is < 0 or > 100) errors.Add("نرخ باید بین ۰ و ۱۰۰ باشد.");
        }
        if (c.Rates.GroupBy(r => r.FromDate).Any(g => g.Count() > 1)) errors.Add("دو نرخ با یک تاریخ شروع نمی‌شود.");
        if (errors.Count > 0) return SalesWriteResult.Fail(errors);

        foreach (var old in await _repo.ListVatRatesAsync(true, ct)) old.IsDeleted = true;
        foreach (var r in c.Rates)
            await _repo.AddVatRateAsync(new SalesVatRate { Id = Guid.NewGuid(), FromDate = r.FromDate, Rate = r.Rate, Note = SalesDefaults.Clean(r.Note, 200), CreatedBy = _user.UserId, CreatedAtUtc = DateTime.UtcNow }, ct);
        await _uow.SaveChangesAsync(ct);
        return new(true, null, []);
    }
}

public static class SalesDates
{
    private static readonly Regex Pattern = new(@"^1[34]\d{2}(0[1-9]|1[0-2])(0[1-9]|[12]\d|3[01])$", RegexOptions.Compiled);

    public static bool IsValid(string? d)
    {
        if (d is null || !Pattern.IsMatch(d)) return false;
        var m = int.Parse(d.Substring(4, 2));
        var day = int.Parse(d.Substring(6, 2));
        return day <= (m <= 6 ? 31 : 30);
    }

    public static string Today() => FiscalYearGuard.Jalali(DateOnly.FromDateTime(DateTime.Now)).Replace("/", "");

    /// <summary>yyyyMMdd شمسی ⇒ DateOnly میلادی.</summary>
    public static DateOnly ToDate(string jalali)
    {
        var pc = new System.Globalization.PersianCalendar();
        return DateOnly.FromDateTime(pc.ToDateTime(int.Parse(jalali[..4]), int.Parse(jalali.Substring(4, 2)), int.Parse(jalali.Substring(6, 2)), 0, 0, 0, 0));
    }

    public static string AddDays(string jalali, int days) =>
        FiscalYearGuard.Jalali(ToDate(jalali).AddDays(days)).Replace("/", "");
}

// ─────────── نقش‌های فروش کاربران واحد ───────────

public sealed record SalesUserRoleDto(Guid Id, string UserId, SalesRole Role);

public sealed record ListSalesRolesQuery : IRequest<IReadOnlyList<SalesUserRoleDto>>, IVahedScopedQuery
{
    [JsonIgnore] public string VahedCode { get; set; } = string.Empty;
}

public sealed class ListSalesRolesHandler : IRequestHandler<ListSalesRolesQuery, IReadOnlyList<SalesUserRoleDto>>
{
    private readonly ISalesRepository _repo;
    private readonly SalesAccess _access;
    public ListSalesRolesHandler(ISalesRepository repo, SalesAccess access) { _repo = repo; _access = access; }

    public async Task<IReadOnlyList<SalesUserRoleDto>> Handle(ListSalesRolesQuery q, CancellationToken ct)
    {
        await _access.RequireAnyAsync(q.VahedCode, ct);
        return (await _repo.ListRolesAsync(q.VahedCode, null, ct)).OrderBy(r => r.UserId).ThenBy(r => r.Role)
            .Select(r => new SalesUserRoleDto(r.Id, r.UserId, r.Role)).ToList();
    }
}

/// <summary>فقط مدیر مالی واحد یا مدیر ستاد نقش می‌دهد (مدیر فروش نمی‌تواند به خودش نقش حسابدار بدهد).</summary>
public sealed record AddSalesRoleCommand(string UserId, SalesRole Role) : IRequest<SalesWriteResult>, IVahedScopedCommand
{
    [JsonIgnore] public string VahedCode { get; set; } = string.Empty;
}

public sealed class AddSalesRoleHandler : IRequestHandler<AddSalesRoleCommand, SalesWriteResult>
{
    private readonly ISalesRepository _repo;
    private readonly SalesAccess _access;
    private readonly IUnitOfWork _uow;
    private readonly ICurrentUser _user;

    public AddSalesRoleHandler(ISalesRepository repo, SalesAccess access, IUnitOfWork uow, ICurrentUser user)
    { _repo = repo; _access = access; _uow = uow; _user = user; }

    public async Task<SalesWriteResult> Handle(AddSalesRoleCommand c, CancellationToken ct)
    {
        if (!(await _access.GetAsync(c.VahedCode, ct)).IsAdmin)
            throw new RoleAccessDeniedException("نقش فروش را فقط مدیر مالی واحد یا مدیر ستاد تعیین می‌کند.");
        var userId = c.UserId?.Trim() ?? "";
        if (!Regex.IsMatch(userId, @"^\d{10}$")) return SalesWriteResult.Fail(["شناسهٔ کاربر (کد ملی ۱۰ رقمی) معتبر نیست."]);
        if (!Enum.IsDefined(c.Role)) return SalesWriteResult.Fail(["نقش نامعتبر است."]);
        if ((await _repo.ListRolesAsync(c.VahedCode, userId, ct)).Any(r => r.Role == c.Role))
            return SalesWriteResult.Fail(["این کاربر این نقش را دارد."]);
        var role = new SalesUserRole { Id = Guid.NewGuid(), VahedCode = c.VahedCode, UserId = userId, Role = c.Role, CreatedBy = _user.UserId, CreatedAtUtc = DateTime.UtcNow };
        await _repo.AddRoleAsync(role, ct);
        await _uow.SaveChangesAsync(ct);
        return SalesWriteResult.Ok(role.Id);
    }
}

public sealed record RemoveSalesRoleCommand : IRequest<Unit>, IVahedScopedCommand
{
    [JsonIgnore] public Guid Id { get; set; }
    [JsonIgnore] public string VahedCode { get; set; } = string.Empty;
}

public sealed class RemoveSalesRoleHandler : IRequestHandler<RemoveSalesRoleCommand, Unit>
{
    private readonly ISalesRepository _repo;
    private readonly SalesAccess _access;
    private readonly IUnitOfWork _uow;

    public RemoveSalesRoleHandler(ISalesRepository repo, SalesAccess access, IUnitOfWork uow) { _repo = repo; _access = access; _uow = uow; }

    public async Task<Unit> Handle(RemoveSalesRoleCommand c, CancellationToken ct)
    {
        if (!(await _access.GetAsync(c.VahedCode, ct)).IsAdmin)
            throw new RoleAccessDeniedException("نقش فروش را فقط مدیر مالی واحد یا مدیر ستاد تعیین می‌کند.");
        var role = await _repo.GetRoleForUpdateAsync(c.Id, ct);
        if (role is null || role.VahedCode != c.VahedCode) throw new NotFoundException("SalesUserRole", c.Id);
        _repo.RemoveRole(role);
        await _uow.SaveChangesAsync(ct);
        return Unit.Value;
    }
}

// ─────────── کالا / خدمت ───────────

public sealed record SalesItemDto(
    Guid Id, string Code, string Name, SalesItemKind Kind, string? GroupName, string UnitName, long Price, long StandardCost,
    string? TaxCode, bool IsExempt, decimal Stock, bool IsActive);

public sealed record ListSalesItemsQuery(string? Search = null, bool ActiveOnly = false) : IRequest<IReadOnlyList<SalesItemDto>>, IVahedScopedQuery
{
    [JsonIgnore] public string VahedCode { get; set; } = string.Empty;
}

public sealed class ListSalesItemsHandler : IRequestHandler<ListSalesItemsQuery, IReadOnlyList<SalesItemDto>>
{
    private readonly ISalesRepository _repo;
    private readonly SalesAccess _access;
    public ListSalesItemsHandler(ISalesRepository repo, SalesAccess access) { _repo = repo; _access = access; }

    public async Task<IReadOnlyList<SalesItemDto>> Handle(ListSalesItemsQuery q, CancellationToken ct)
    {
        await _access.RequireAnyAsync(q.VahedCode, ct);
        return (await _repo.ListItemsAsync(q.VahedCode, q.Search, q.ActiveOnly, ct)).Select(ToDto).ToList();
    }

    internal static SalesItemDto ToDto(SalesItem i) => new(i.Id, i.Code, i.Name, i.Kind, i.GroupName, i.UnitName, i.Price, i.StandardCost, i.TaxCode, i.IsExempt, i.Stock, i.IsActive);
}

/// <param name="Stock">فقط هنگام ایجاد (موجودی اول دوره)؛ بعد از آن موجودی فقط با حواله/فاکتور/برگشت تغییر می‌کند.</param>
public sealed record SaveSalesItemCommand(
    string Code, string Name, SalesItemKind Kind, string? GroupName, string UnitName, long Price, long StandardCost,
    string? TaxCode, bool IsExempt, bool IsActive, decimal Stock = 0) : IRequest<SalesWriteResult>, IVahedScopedCommand
{
    [JsonIgnore] public Guid? Id { get; set; }
    [JsonIgnore] public string VahedCode { get; set; } = string.Empty;
}

public sealed class SaveSalesItemHandler : IRequestHandler<SaveSalesItemCommand, SalesWriteResult>
{
    private readonly ISalesRepository _repo;
    private readonly SalesAccess _access;
    private readonly IUnitOfWork _uow;
    private readonly ICurrentUser _user;

    public SaveSalesItemHandler(ISalesRepository repo, SalesAccess access, IUnitOfWork uow, ICurrentUser user)
    { _repo = repo; _access = access; _uow = uow; _user = user; }

    public async Task<SalesWriteResult> Handle(SaveSalesItemCommand c, CancellationToken ct)
    {
        await _access.RequireManageAsync(c.VahedCode, ct);
        var code = c.Code?.Trim().ToUpperInvariant() ?? "";
        var errors = new List<string>();
        if (!Regex.IsMatch(code, @"^[A-Z0-9\-_]{1,30}$")) errors.Add("کد کالا ۱ تا ۳۰ حرف لاتین، رقم، «-» یا «_» است.");
        if (string.IsNullOrWhiteSpace(c.Name) || c.Name.Trim().Length > 200) errors.Add("نام کالا الزامی و حداکثر ۲۰۰ کاراکتر است.");
        if (string.IsNullOrWhiteSpace(c.UnitName) || c.UnitName.Trim().Length > 30) errors.Add("واحد سنجش الزامی است.");
        if (!Enum.IsDefined(c.Kind)) errors.Add("نوع کالا نامعتبر است.");
        if (c.Price < 0 || c.StandardCost < 0) errors.Add("قیمت و بها منفی نمی‌شوند.");
        if (!string.IsNullOrWhiteSpace(c.TaxCode) && !Regex.IsMatch(c.TaxCode.Trim(), @"^\d{13}$")) errors.Add("شناسهٔ کالای مودیان ۱۳ رقم است.");
        if (c.Stock < 0) errors.Add("موجودی اول دوره منفی نمی‌شود.");
        if (errors.Count == 0 && await _repo.ItemCodeExistsAsync(c.VahedCode, code, c.Id, ct)) errors.Add($"کد «{code}» قبلاً ثبت شده است.");
        if (errors.Count > 0) return SalesWriteResult.Fail(errors);

        SalesItem item;
        if (c.Id is { } id)
        {
            item = (await _repo.GetItemsAsync([id], true, ct)).GetValueOrDefault(id) ?? throw new NotFoundException("SalesItem", id);
            if (item.VahedCode != c.VahedCode || item.IsDeleted) throw new NotFoundException("SalesItem", id);
        }
        else
        {
            item = new SalesItem { Id = Guid.NewGuid(), VahedCode = c.VahedCode, CreatedBy = _user.UserId, CreatedAtUtc = DateTime.UtcNow, Stock = c.Kind == SalesItemKind.Goods ? c.Stock : 0 };
            await _repo.AddItemAsync(item, ct);
        }
        item.Code = code;
        item.Name = c.Name.Trim();
        item.Kind = c.Kind;
        item.GroupName = SalesDefaults.Clean(c.GroupName, 100);
        item.UnitName = c.UnitName.Trim();
        item.Price = c.Price;
        item.StandardCost = c.Kind == SalesItemKind.Goods ? c.StandardCost : 0;
        item.TaxCode = SalesDefaults.Clean(c.TaxCode, 13);
        item.IsExempt = c.IsExempt;
        item.IsActive = c.IsActive;
        await _uow.SaveChangesAsync(ct);
        return SalesWriteResult.Ok(item.Id);
    }
}

// ─────────── مشتری (پروفایل فروش روی تفصیلی) ───────────

public sealed record SalesCustomerDto(
    Guid Id, Guid TafsiliId, string? TafsiliCode, string? Name, PersonKind PersonKind, string? NationalId, string? EconomicCode,
    string? PostalCode, long CreditLimit, int PayTerms, bool IsBlocked, bool CompleteForMoadian);

public sealed record ListSalesCustomersQuery(string? Search = null) : IRequest<IReadOnlyList<SalesCustomerDto>>, IVahedScopedQuery
{
    [JsonIgnore] public string VahedCode { get; set; } = string.Empty;
}

public sealed class ListSalesCustomersHandler : IRequestHandler<ListSalesCustomersQuery, IReadOnlyList<SalesCustomerDto>>
{
    private readonly ISalesRepository _repo;
    private readonly SalesAccess _access;
    public ListSalesCustomersHandler(ISalesRepository repo, SalesAccess access) { _repo = repo; _access = access; }

    public async Task<IReadOnlyList<SalesCustomerDto>> Handle(ListSalesCustomersQuery q, CancellationToken ct)
    {
        await _access.RequireAnyAsync(q.VahedCode, ct);
        return (await _repo.ListCustomersAsync(q.VahedCode, q.Search, null, ct)).Select(ToDto).ToList();
    }

    internal static SalesCustomerDto ToDto(SalesCustomerRow r)
    {
        var c = r.Customer;
        return new(c.Id, c.TafsiliId, r.TafsiliCode, r.TafsiliName, c.PersonKind, c.NationalId, c.EconomicCode, c.PostalCode,
            c.CreditLimit, c.PayTerms, c.IsBlocked, SalesIds.CompleteForMoadian(c));
    }
}

public sealed record SaveSalesCustomerCommand(
    Guid TafsiliId, PersonKind PersonKind, string? NationalId, string? EconomicCode, string? PostalCode,
    long CreditLimit, int PayTerms, bool IsBlocked) : IRequest<SalesWriteResult>, IVahedScopedCommand
{
    [JsonIgnore] public Guid? Id { get; set; }
    [JsonIgnore] public string VahedCode { get; set; } = string.Empty;
}

public sealed class SaveSalesCustomerHandler : IRequestHandler<SaveSalesCustomerCommand, SalesWriteResult>
{
    private readonly ISalesRepository _repo;
    private readonly SalesAccess _access;
    private readonly IDetailReader _details;
    private readonly IUnitOfWork _uow;
    private readonly ICurrentUser _user;

    public SaveSalesCustomerHandler(ISalesRepository repo, SalesAccess access, IDetailReader details, IUnitOfWork uow, ICurrentUser user)
    { _repo = repo; _access = access; _details = details; _uow = uow; _user = user; }

    public async Task<SalesWriteResult> Handle(SaveSalesCustomerCommand c, CancellationToken ct)
    {
        var access = await _access.RequireAnyAsync(c.VahedCode, ct);
        var errors = new List<string>();
        var nid = SalesIds.Digits(c.NationalId);
        var postal = SalesIds.Digits(c.PostalCode);
        if (!Enum.IsDefined(c.PersonKind)) errors.Add("نوع شخص نامعتبر است.");
        if (nid is not null && !(c.PersonKind == PersonKind.Legal ? SalesIds.ValidLegalId(nid) : SalesIds.ValidNationalCode(nid)))
            errors.Add(c.PersonKind == PersonKind.Legal ? "شناسهٔ ملی معتبر نیست (۱۱ رقم با رقم کنترلی)." : "کد ملی معتبر نیست.");
        if (postal is not null && postal.Length != 10) errors.Add("کد پستی ۱۰ رقم است.");
        if (c.CreditLimit < 0 || c.PayTerms is < 0 or > 3650) errors.Add("سقف اعتبار یا مهلت پرداخت نامعتبر است.");

        var detail = (await _details.GetByIdsAsync([c.TafsiliId], c.VahedCode, ct)).GetValueOrDefault(c.TafsiliId);
        if (detail is null || !detail.IsActive || !detail.IsVisibleToUnit) errors.Add("تفصیلی انتخاب‌شده برای این واحد فعال یا قابل استفاده نیست.");
        if (errors.Count == 0 && await _repo.CustomerExistsForTafsiliAsync(c.VahedCode, c.TafsiliId, c.Id, ct)) errors.Add("برای این تفصیلی قبلاً پروفایل مشتری ساخته شده است.");
        if (errors.Count == 0 && nid is not null && await _repo.NationalIdExistsAsync(c.VahedCode, nid, c.Id, ct)) errors.Add("مشتری دیگری با این شناسه/کد ملی ثبت شده است.");
        if (errors.Count > 0) return SalesWriteResult.Fail(errors);

        SalesCustomer customer;
        if (c.Id is { } id)
        {
            customer = await _repo.GetCustomerAsync(id, true, ct) ?? throw new NotFoundException("SalesCustomer", id);
            if (customer.VahedCode != c.VahedCode || customer.IsDeleted) throw new NotFoundException("SalesCustomer", id);
        }
        else
        {
            customer = new SalesCustomer { Id = Guid.NewGuid(), VahedCode = c.VahedCode, CreatedBy = _user.UserId, CreatedAtUtc = DateTime.UtcNow };
            await _repo.AddCustomerAsync(customer, ct);
        }
        // سقف اعتبار و مسدودی کنترل اعتباری‌اند: فقط مدیر فروش/مدیر تغییرشان می‌دهد.
        if (!access.CanManage && (c.Id is null ? c.CreditLimit > 0 || c.IsBlocked : customer.CreditLimit != c.CreditLimit || customer.IsBlocked != c.IsBlocked))
            throw new RoleAccessDeniedException("سقف اعتبار و مسدودی مشتری را فقط مدیر فروش تعیین می‌کند.");

        customer.TafsiliId = c.TafsiliId;
        customer.PersonKind = c.PersonKind;
        customer.NationalId = nid;
        customer.EconomicCode = SalesIds.Digits(c.EconomicCode) ?? (c.PersonKind == PersonKind.Legal ? nid : null);
        customer.PostalCode = postal;
        customer.CreditLimit = c.CreditLimit;
        customer.PayTerms = c.PayTerms;
        customer.IsBlocked = c.IsBlocked;
        await _uow.SaveChangesAsync(ct);
        return SalesWriteResult.Ok(customer.Id);
    }
}

public static class SalesIds
{
    public static string? Digits(string? s)
    {
        if (string.IsNullOrWhiteSpace(s)) return null;
        var d = new string(s.Trim().Select(ch => ch is >= '۰' and <= '۹' ? (char)('0' + ch - '۰') : ch is >= '٠' and <= '٩' ? (char)('0' + ch - '٠') : ch).ToArray());
        return d.Length == 0 ? null : d;
    }

    public static bool ValidNationalCode(string c)
    {
        if (!Regex.IsMatch(c, @"^\d{10}$") || c.Distinct().Count() == 1) return false;
        var s = Enumerable.Range(0, 9).Sum(i => (c[i] - '0') * (10 - i));
        var r = s % 11;
        return c[9] - '0' == (r < 2 ? r : 11 - r);
    }

    public static bool ValidLegalId(string c)
    {
        if (!Regex.IsMatch(c, @"^\d{11}$")) return false;
        var d = c[9] - '0' + 2;
        int[] co = [29, 27, 23, 19, 17];
        var s = Enumerable.Range(0, 10).Sum(i => (c[i] - '0' + d) * co[i % 5]);
        var r = s % 11;
        if (r == 10) r = 0;
        return r == c[10] - '0';
    }

    /// <summary>صورتحساب نوع اول: شناسه/کد ملی یا کد اقتصادی + کد پستی ۱۰ رقمی.</summary>
    public static bool CompleteForMoadian(SalesCustomer c) =>
        (c.NationalId is not null || c.EconomicCode is not null) && c.PostalCode is { Length: 10 };
}
