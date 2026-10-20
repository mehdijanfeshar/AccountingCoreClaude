using Accounting.Application.Common.Interfaces;
using Accounting.Application.FinancialStatements.Periods;
using Accounting.Application.OperationTemplates;
using Accounting.Application.Sales;
using Accounting.Domain.Entity;
using Accounting.Domain.Sales;
using Accounting.Domain.ValueObjects;
using Moq;

namespace Accounting.Application.Tests.Sales;

/// <summary>فاز ۵۹ — قواعد سند فروش: تبدیل، باقی‌مانده، کنترل‌ها و سند حسابداری.</summary>
public class SalesDocEngineTests
{
    private const string Vahed = "0101";
    private const string Year = "1405";
    private const string Date = "14050720";

    private readonly Mock<ISalesRepository> _repo = new();
    private readonly Mock<ISalesLedgerReader> _ledger = new();
    private readonly Mock<ISubsidiaryAccountReader> _accounts = new();
    private readonly Mock<IDetailReader> _details = new();
    private readonly Mock<IUnitAccessReadRepository> _units = new();
    private readonly Mock<IFsPeriodRepository> _periods = new();

    private readonly SalesItem _goods = new() { Id = Guid.NewGuid(), VahedCode = Vahed, Name = "کالا", Kind = SalesItemKind.Goods, Stock = 10, StandardCost = 600 };
    private readonly SalesItem _service = new() { Id = Guid.NewGuid(), VahedCode = Vahed, Name = "خدمت", Kind = SalesItemKind.Service };
    private readonly SalesCustomer _customer = new() { Id = Guid.NewGuid(), VahedCode = Vahed, TafsiliId = Guid.NewGuid() };
    private readonly Dictionary<string, SalesAccountMap> _maps;
    private readonly List<SalesDoc> _children = [];
    private readonly List<TB_FS_PERIOD> _periodRows = [];

    public SalesDocEngineTests()
    {
        _maps = SalesAccountRoles.All.ToDictionary(r => r, r => new SalesAccountMap { RoleKey = r, AccountId = Guid.NewGuid(), VahedCode = Vahed });
        _repo.Setup(r => r.GetChildrenAsync(It.IsAny<IReadOnlyCollection<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyCollection<Guid> ids, CancellationToken _) => _children.Where(c => c.SourceId is { } s && ids.Contains(s)).ToList());
        _repo.Setup(r => r.GetApprovedOrdersAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync([]);
        _accounts.Setup(a => a.GetByIdsAsync(It.IsAny<IReadOnlyCollection<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyCollection<Guid> ids, CancellationToken _) =>
                ids.ToDictionary(id => id, id => new SubsidiaryAccountInfo(id, "1101", "حساب", true, [])));
        _details.Setup(d => d.GetByIdsAsync(It.IsAny<IReadOnlyCollection<Guid>>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<Guid, DetailInfo>());
        _units.Setup(u => u.GetAllUnitsAsync(It.IsAny<CancellationToken>())).ReturnsAsync([]);
        _periods.Setup(p => p.GetForYearAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(() => _periodRows);
    }

    private SalesDocEngine Engine() => new(_repo.Object, _ledger.Object, _accounts.Object, _details.Object,
        new AssistantUnitPolicy(_units.Object, new FsPeriodGuard(_periods.Object, _units.Object)));

    private SalesContext Ctx(SalesSetting? s = null, bool withMaps = true) =>
        new(s ?? new SalesSetting { VahedCode = Vahed }, withMaps ? _maps : new Dictionary<string, SalesAccountMap>(), []);

    private Dictionary<Guid, SalesItem> Items => new() { [_goods.Id] = _goods, [_service.Id] = _service };

    private SalesCustomerRow CustomerRow => new(_customer, "1", "مشتری");

    private SalesDoc Doc(SalesDocType type, SalesDocStatus status = SalesDocStatus.Draft, Guid? source = null, params SalesDocLine[] lines)
    {
        var d = new SalesDoc
        {
            Id = Guid.NewGuid(), VahedCode = Vahed, Year = Year, DocType = type, DocDate = Date, CustomerId = _customer.Id,
            SourceId = source, Status = status, Lines = lines.ToList(),
        };
        var t = SalesCalculator.Apply(d.Lines, d.HeaderDiscount, 10, _ => false);
        (d.Gross, d.Discount, d.Taxable, d.Vat, d.Total) = (t.Gross, t.Discount, t.Taxable, t.Vat, t.Total);
        return d;
    }

    private static SalesDocLine Line(SalesItem item, decimal qty, long price = 1_000, decimal discount = 0) =>
        new() { Id = Guid.NewGuid(), ItemId = item.Id, Qty = qty, Price = price, DiscountPercent = discount, Radif = 1 };

    private SalesDoc Source(SalesDoc d)
    {
        _repo.Setup(r => r.GetDocAsync(d.Id, false, It.IsAny<CancellationToken>())).ReturnsAsync(d);
        return d;
    }

    private Task<(List<SalesCheck> Checks, List<string> Reasons)> Check(SalesDoc d, SalesSetting? s = null, bool forPosting = false, SalesCustomerRow? customer = null) =>
        Engine().CheckAsync(d, Ctx(s), Items, customer ?? CustomerRow, forPosting, Year, default);

    private static bool HasErr(List<SalesCheck> checks, string fragment) => checks.Any(c => c.Level == "err" && c.Message.Contains(fragment));

    // ─────────── تبدیل ───────────

    [Fact]
    public void Targets_FollowTheDocumentChain()
    {
        Assert.Equal([SalesDocType.Order], SalesDocEngine.TargetsOf(SalesDocType.ProForma));
        Assert.Equal([SalesDocType.Delivery, SalesDocType.Invoice], SalesDocEngine.TargetsOf(SalesDocType.Order));
        Assert.Equal([SalesDocType.Invoice], SalesDocEngine.TargetsOf(SalesDocType.Delivery));
        Assert.Equal([SalesDocType.Return], SalesDocEngine.TargetsOf(SalesDocType.Invoice));
        Assert.Empty(SalesDocEngine.TargetsOf(SalesDocType.Return));
    }

    [Theory]
    [InlineData(SalesDocType.ProForma, SalesDocStatus.Approved, true)]
    [InlineData(SalesDocType.Order, SalesDocStatus.Posted, false)]
    [InlineData(SalesDocType.Delivery, SalesDocStatus.Posted, true)]
    [InlineData(SalesDocType.Invoice, SalesDocStatus.Approved, false)]
    public void Convertible_NeedsApprovedOrPosted(SalesDocType type, SalesDocStatus status, bool expected) =>
        Assert.Equal(expected, SalesDocEngine.IsConvertible(new SalesDoc { DocType = type, Status = status }));

    // ─────────── باقی‌مانده ───────────

    [Fact]
    public async Task Remaining_ProFormaToOrder_SubtractsLiveChildrenButNotTheEditedOne()
    {
        var pf = Doc(SalesDocType.ProForma, SalesDocStatus.Approved, null, Line(_goods, 10));
        var draft = Doc(SalesDocType.Order, source: pf.Id, lines: Line(_goods, 3));
        var editing = Doc(SalesDocType.Order, source: pf.Id, lines: Line(_goods, 4));
        _children.AddRange([draft, editing]);

        var rem = await Engine().RemainingAsync(pf, SalesDocType.Order, editing.Id, false, Items, default);

        Assert.Equal(7, rem[_goods.Id]);
    }

    [Fact]
    public async Task Remaining_OrderToDelivery_ServicesAreNotDelivered()
    {
        var so = Doc(SalesDocType.Order, SalesDocStatus.Approved, null, Line(_goods, 5), Line(_service, 2));
        var rem = await Engine().RemainingAsync(so, SalesDocType.Delivery, null, false, Items, default);
        Assert.Equal(5, rem[_goods.Id]);
        Assert.False(rem.ContainsKey(_service.Id));
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(true, true)]
    public async Task Remaining_OrderToInvoice_GoodsOnlyWhenBillBeforeDelivery(bool billBeforeDelivery, bool goodsBillable)
    {
        var so = Doc(SalesDocType.Order, SalesDocStatus.Approved, null, Line(_goods, 5), Line(_service, 2));
        var rem = await Engine().RemainingAsync(so, SalesDocType.Invoice, null, billBeforeDelivery, Items, default);
        Assert.Equal(2, rem[_service.Id]);
        Assert.Equal(goodsBillable, rem.ContainsKey(_goods.Id));
    }

    [Fact]
    public async Task Remaining_DeliveryToInvoice_CappedByWhatTheOrderAlreadyInvoiced()
    {
        var so = Source(Doc(SalesDocType.Order, SalesDocStatus.Approved, null, Line(_goods, 10)));
        var dn = Doc(SalesDocType.Delivery, SalesDocStatus.Posted, so.Id, Line(_goods, 6));
        var direct = Doc(SalesDocType.Invoice, SalesDocStatus.Posted, so.Id, Line(_goods, 7)); // فاکتور پیش از تحویل
        _children.AddRange([dn, direct]);

        var rem = await Engine().RemainingAsync(dn, SalesDocType.Invoice, null, true, Items, default);

        Assert.Equal(3, rem[_goods.Id]); // min(۶ تحویل, ۱۰ − ۷ فاکتورشده)
    }

    // ─────────── کنترل‌ها ───────────

    [Fact]
    public async Task Check_CleanProForma_HasNoErrors()
    {
        var (checks, reasons) = await Check(Doc(SalesDocType.ProForma, lines: Line(_goods, 1)));
        Assert.DoesNotContain(checks, c => c.Level == "err");
        Assert.Empty(reasons);
    }

    [Fact]
    public async Task Check_MissingOrBlockedCustomer()
    {
        var d = Doc(SalesDocType.ProForma, lines: Line(_goods, 1));
        Assert.True(HasErr((await Engine().CheckAsync(d, Ctx(), Items, null, false, Year, default)).Checks, "مشتری"));

        _customer.IsBlocked = true;
        Assert.True(HasErr((await Check(d)).Checks, "مسدود"));
    }

    [Fact]
    public async Task Check_InvalidDateAndWrongFiscalYear()
    {
        var bad = Doc(SalesDocType.ProForma, lines: Line(_goods, 1));
        bad.DocDate = "14050731"; // مهر ۳۰ روزه است
        Assert.True(HasErr((await Check(bad)).Checks, "تاریخ سند معتبر نیست"));

        var otherYear = Doc(SalesDocType.ProForma, lines: Line(_goods, 1));
        otherYear.DocDate = "14040720";
        Assert.True(HasErr((await Check(otherYear)).Checks, "سال مالی"));
    }

    [Fact]
    public async Task Check_LineRules()
    {
        Assert.True(HasErr((await Check(Doc(SalesDocType.ProForma))).Checks, "حداقل یک کالا"));
        Assert.True(HasErr((await Check(Doc(SalesDocType.ProForma, lines: Line(_goods, 0)))).Checks, "بیشتر از صفر"));
        Assert.True(HasErr((await Check(Doc(SalesDocType.ProForma, lines: [Line(_goods, 1), Line(_goods, 2)]))).Checks, "تکرار"));

        var foreign = new SalesItem { Id = Guid.NewGuid(), VahedCode = "9999", Name = "x" };
        var d = Doc(SalesDocType.ProForma, lines: new SalesDocLine { ItemId = foreign.Id, Qty = 1, Price = 1, Radif = 1 });
        var items = Items;
        items[foreign.Id] = foreign;
        Assert.True(HasErr((await Engine().CheckAsync(d, Ctx(), items, CustomerRow, false, Year, default)).Checks, "پیدا نشد"));
    }

    [Fact]
    public async Task Check_DeliveryAndReturnNeedSource()
    {
        Assert.True(HasErr((await Check(Doc(SalesDocType.Delivery, lines: Line(_goods, 1)))).Checks, "از سفارش فروش"));
        Assert.True(HasErr((await Check(Doc(SalesDocType.Return, lines: Line(_goods, 1)))).Checks, "فاکتور اصلی"));
    }

    [Fact]
    public async Task Check_ServiceInDelivery_IsError()
    {
        var so = Source(Doc(SalesDocType.Order, SalesDocStatus.Approved, null, Line(_service, 1)));
        Assert.True(HasErr((await Check(Doc(SalesDocType.Delivery, source: so.Id, lines: Line(_service, 1)))).Checks, "حواله انبار"));
    }

    [Fact]
    public async Task Check_ConversionBeyondRemaining_AndDifferentCustomer()
    {
        var so = Source(Doc(SalesDocType.Order, SalesDocStatus.Approved, null, Line(_goods, 2)));
        var dn = Doc(SalesDocType.Delivery, source: so.Id, lines: Line(_goods, 3));
        Assert.True(HasErr((await Check(dn)).Checks, "بیش از باقی‌مانده"));

        dn.CustomerId = Guid.NewGuid();
        Assert.True(HasErr((await Check(dn)).Checks, "همان مشتری"));
    }

    [Fact]
    public async Task Check_UnapprovedSource_IsInvalid()
    {
        var so = Source(Doc(SalesDocType.Order, SalesDocStatus.Draft, null, Line(_goods, 2)));
        Assert.True(HasErr((await Check(Doc(SalesDocType.Delivery, source: so.Id, lines: Line(_goods, 1)))).Checks, "سند مبدأ"));
    }

    [Fact]
    public async Task Check_InsufficientStock_WarnsOnSave_BlocksOnPosting()
    {
        var d = Doc(SalesDocType.Invoice, lines: Line(_goods, 11)); // فاکتور مستقیم کالا خارج می‌کند
        Assert.Contains((await Check(d)).Checks, c => c.Level == "warn" && c.Message.Contains("موجودی"));
        Assert.True(HasErr((await Check(d, forPosting: true)).Checks, "موجودی"));
        Assert.DoesNotContain((await Check(d, new SalesSetting { AllowNegativeStock = true }, true)).Checks, c => c.Message.Contains("موجودی"));
    }

    [Fact]
    public async Task Check_DiscountAboveThreshold_NeedsApproval()
    {
        var (checks, reasons) = await Check(Doc(SalesDocType.ProForma, lines: Line(_goods, 1, discount: 15)),
            new SalesSetting { DiscountApprovalPercent = 10 });
        Assert.Single(reasons);
        Assert.DoesNotContain(checks, c => c.Level == "err");
    }

    [Fact]
    public async Task Check_ConvertedDoc_DiscountIsNotRechecked()
    {
        var pf = Source(Doc(SalesDocType.ProForma, SalesDocStatus.Approved, null, Line(_goods, 1, discount: 50)));
        var (_, reasons) = await Check(Doc(SalesDocType.Order, source: pf.Id, lines: Line(_goods, 1, discount: 50)),
            new SalesSetting { DiscountApprovalPercent = 10 });
        Assert.Empty(reasons);
    }

    [Theory]
    [InlineData(CreditPolicy.Block, "err", 0)]
    [InlineData(CreditPolicy.Approve, "warn", 1)]
    [InlineData(CreditPolicy.Warn, "warn", 0)]
    public async Task Check_CreditLimit_FollowsPolicy(CreditPolicy policy, string level, int reasonCount)
    {
        _customer.CreditLimit = 1_500;
        _ledger.Setup(l => l.ReceivableBalanceAsync(Vahed, Year, _maps[SalesAccountRoles.Receivable].AccountId, _customer.TafsiliId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(1_000);

        var (checks, reasons) = await Check(Doc(SalesDocType.Order, lines: Line(_goods, 1)), new SalesSetting { CreditPolicy = policy });

        Assert.Contains(checks, c => c.Level == level && c.Message.Contains("سقف اعتبار"));
        Assert.Equal(reasonCount, reasons.Count);
    }

    [Fact]
    public async Task Exposure_CountsOpenPartOfApprovedOrders()
    {
        var so = Doc(SalesDocType.Order, SalesDocStatus.Approved, null, Line(_goods, 4)); // جمع ۴٬۴۰۰
        _children.Add(Doc(SalesDocType.Invoice, SalesDocStatus.Posted, so.Id, Line(_goods, 1)));
        _repo.Setup(r => r.GetApprovedOrdersAsync(_customer.Id, It.IsAny<CancellationToken>())).ReturnsAsync([so]);

        var (_, open) = await Engine().ExposureAsync(_customer, Ctx(), null, Year, default);

        Assert.Equal(3_300, open); // ۳ از ۴ فاکتور نشده
    }

    [Fact]
    public async Task Check_LockedPeriod_BlocksPostingOnly()
    {
        _periodRows.Add(new TB_FS_PERIOD { VAHEDCODE = Vahed, YEAR = Year, STATE = FsPeriodState.Locked });
        var d = Doc(SalesDocType.ProForma, lines: Line(_goods, 1));
        Assert.DoesNotContain((await Check(d)).Checks, c => c.Message.Contains("قفل"));
        Assert.True(HasErr((await Check(d, forPosting: true)).Checks, "قفل"));
    }

    [Fact]
    public async Task Check_Moadian_LegalBuyerWithIncompleteData_Warns()
    {
        var d = Doc(SalesDocType.Invoice, lines: Line(_goods, 1));
        Assert.Contains((await Check(d)).Checks, c => c.Level == "warn" && c.Message.Contains("حقوقی"));

        _customer.NationalId = "10100000000";
        _customer.PostalCode = "1234567890";
        Assert.Contains((await Check(d)).Checks, c => c.Level == "ok" && c.Message.Contains("نوع اول"));
    }

    // ─────────── سند حسابداری ───────────

    private static long Dr(SalesJournal j, string role) => j.Lines.Where(l => l.RoleKey == role).Sum(l => l.Debit);
    private static long Cr(SalesJournal j, string role) => j.Lines.Where(l => l.RoleKey == role).Sum(l => l.Credit);

    [Fact]
    public async Task Journal_DirectInvoice_RevenueVatDiscountAndCostOfGoods()
    {
        var d = Doc(SalesDocType.Invoice, lines: [Line(_goods, 2, 1_000, discount: 10), Line(_service, 1, 500)]);

        var j = await Engine().JournalAsync(d, Ctx(), Items, _customer, default);

        Assert.Empty(j.Errors);
        Assert.Equal(j.Debit, j.Credit);
        Assert.Equal(d.Total, Dr(j, SalesAccountRoles.Receivable));
        Assert.Equal(200, Dr(j, SalesAccountRoles.Discount));
        Assert.Equal(2_000, Cr(j, SalesAccountRoles.Sales));
        Assert.Equal(500, Cr(j, SalesAccountRoles.Service));
        Assert.Equal(d.Vat, Cr(j, SalesAccountRoles.Vat));
        Assert.Equal(1_200, Dr(j, SalesAccountRoles.Cogs));      // ۲ × بهای استاندارد ۶۰۰
        Assert.Equal(1_200, Cr(j, SalesAccountRoles.Inventory));
    }

    [Fact]
    public async Task Journal_InvoiceFromOrder_GoodsGoToContractLiability_NoCost()
    {
        var so = Source(Doc(SalesDocType.Order, SalesDocStatus.Approved, null, Line(_goods, 2)));
        var d = Doc(SalesDocType.Invoice, source: so.Id, lines: Line(_goods, 2));

        var j = await Engine().JournalAsync(d, Ctx(), Items, _customer, default);

        Assert.Empty(j.Errors);
        Assert.Equal(2_000, Cr(j, SalesAccountRoles.Contract));
        Assert.Equal(0, Cr(j, SalesAccountRoles.Sales));
        Assert.Equal(0, Dr(j, SalesAccountRoles.Cogs));
        Assert.Equal(j.Debit, j.Credit);
    }

    [Fact]
    public async Task Journal_Return_ReversesRevenueVatAndCost()
    {
        var d = Doc(SalesDocType.Return, source: Guid.NewGuid(), lines: Line(_goods, 1));
        var j = await Engine().JournalAsync(d, Ctx(), Items, _customer, default);

        Assert.Empty(j.Errors);
        Assert.Equal(d.Taxable, Dr(j, SalesAccountRoles.Return));
        Assert.Equal(d.Total, Cr(j, SalesAccountRoles.Receivable));
        Assert.Equal(600, Dr(j, SalesAccountRoles.Inventory));
        Assert.Equal(600, Cr(j, SalesAccountRoles.Cogs));
        Assert.Equal(j.Debit, j.Credit);
    }

    [Fact]
    public async Task Journal_MissingAccountMap_IsError()
    {
        var d = Doc(SalesDocType.Delivery, source: Guid.NewGuid(), lines: Line(_goods, 1));
        var j = await Engine().JournalAsync(d, Ctx(withMaps: false), Items, _customer, default);
        Assert.Empty(j.Lines);
        Assert.Contains(j.Errors, e => e.Contains("بهای تمام‌شده"));
    }

    [Fact]
    public async Task Journal_InactiveAccount_IsError()
    {
        _accounts.Setup(a => a.GetByIdsAsync(It.IsAny<IReadOnlyCollection<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyCollection<Guid> ids, CancellationToken _) =>
                ids.ToDictionary(id => id, id => new SubsidiaryAccountInfo(id, "1101", "حساب", false, [])));
        var d = Doc(SalesDocType.Delivery, source: Guid.NewGuid(), lines: Line(_goods, 1));
        var j = await Engine().JournalAsync(d, Ctx(), Items, _customer, default);
        Assert.Contains(j.Errors, e => e.Contains("فعال نیست"));
    }

    [Fact]
    public async Task Journal_CustomerDetailAttachedWhereGroupMatches_RequiredLevelWithoutDetailIsError()
    {
        var group = Guid.NewGuid();
        var arId = _maps[SalesAccountRoles.Receivable].AccountId;
        _accounts.Setup(a => a.GetByIdsAsync(It.IsAny<IReadOnlyCollection<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyCollection<Guid> ids, CancellationToken _) => ids.ToDictionary(id => id, id =>
                new SubsidiaryAccountInfo(id, "1101", "حساب", true,
                    id == arId ? [new DetailLevelRule(1, Guid.NewGuid(), new HashSet<Guid> { group }, true)] : [])));
        _details.Setup(d => d.GetByIdsAsync(It.IsAny<IReadOnlyCollection<Guid>>(), Vahed, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<Guid, DetailInfo>
            {
                [_customer.TafsiliId] = new(_customer.TafsiliId, "1", "مشتری", new HashSet<Guid> { group }, true, true),
            });

        var d = Doc(SalesDocType.Invoice, lines: Line(_service, 1));
        var j = await Engine().JournalAsync(d, Ctx(), Items, _customer, default);
        Assert.Empty(j.Errors);
        Assert.Equal(_customer.TafsiliId, Assert.Single(j.Lines.Single(l => l.RoleKey == SalesAccountRoles.Receivable).Details).DetailId);

        _details.Setup(x => x.GetByIdsAsync(It.IsAny<IReadOnlyCollection<Guid>>(), Vahed, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<Guid, DetailInfo>());
        j = await Engine().JournalAsync(d, Ctx(), Items, _customer, default);
        Assert.Contains(j.Errors, e => e.Contains("تفصیلی سطح 1"));
    }
}
