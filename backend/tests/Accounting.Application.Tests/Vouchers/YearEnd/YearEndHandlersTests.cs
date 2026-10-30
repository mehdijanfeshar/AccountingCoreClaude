using Accounting.Application.Common.Exceptions;
using Accounting.Application.Common.Interfaces;
using Accounting.Application.Vouchers.YearEnd;
using Accounting.Domain.Entity;
using Accounting.Domain.ValueObjects;
using Moq;

namespace Accounting.Application.Tests.Vouchers.YearEnd;

/// <summary>
/// پیش‌نمایش و صدور سند افتتاحیه/اختتامیه (فاز ۵۵): مشکلات مانع صدور، هشدارها، تاریخ و سال سند،
/// شماره‌گذاری پیاپی سندهای یک دسته، FLAG_STATE اختتامیه و تفصیلی ردیف‌ها.
/// </summary>
public class YearEndHandlersTests
{
    private const string Vahed = "0042";

    private static readonly Guid VahedType = Guid.NewGuid();
    private static readonly YearEndAccountRef Iface = new(Guid.NewGuid(), "990001", "رابط افتتاحیه");
    private static readonly YearEndAccountRef Rabet = new(Guid.NewGuid(), "990002", "رابط اختتامیه");

    private readonly Mock<IYearEndRepository> _repo = new();
    private readonly Mock<IVoucherHeadRepository> _heads = new();
    private readonly Mock<ICurrentUser> _user = new();
    private readonly Mock<IUnitOfWork> _uow = new();
    private readonly List<(TB_VOUCHERSHEAD Head, IReadOnlyList<TB_VOUCHERSDETAIL> Details, IReadOnlyList<TB_VOUCHERDETAIL_LINK_TAFSILI> Links)> _staged = new();

    public YearEndHandlersTests()
    {
        _user.SetupGet(u => u.UserId).Returns("1234567890");
        _repo.Setup(r => r.GetVahedTypeIdAsync(Vahed, It.IsAny<CancellationToken>())).ReturnsAsync(VahedType);
        _repo.Setup(r => r.GetExceptionAccountIdsAsync(VahedType, It.IsAny<CancellationToken>())).ReturnsAsync(new HashSet<Guid>());
        _repo.Setup(r => r.GetInterfaceAccountAsync(InterfaceType.OpenVoucher, It.IsAny<CancellationToken>())).ReturnsAsync(Iface);
        _repo.Setup(r => r.GetDocNumsWithAccountAsync(Vahed, It.IsAny<string>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync(Array.Empty<string>());
        _repo.Setup(r => r.GetClosingDocNumsAsync(Vahed, It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(Array.Empty<string>());
        _repo.Setup(r => r.GetClosingRabetMapAsync(VahedType, It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<Guid, YearEndAccountRef>());
        _repo.Setup(r => r.GetBalancesAsync(Vahed, It.IsAny<string>(), It.IsAny<IReadOnlyCollection<char>>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<YearEndBalance>());
        _repo.Setup(r => r.StageAsync(It.IsAny<TB_VOUCHERSHEAD>(), It.IsAny<IReadOnlyList<TB_VOUCHERSDETAIL>>(), It.IsAny<IReadOnlyList<TB_VOUCHERDETAIL_LINK_TAFSILI>>(), It.IsAny<CancellationToken>()))
            .Callback<TB_VOUCHERSHEAD, IReadOnlyList<TB_VOUCHERSDETAIL>, IReadOnlyList<TB_VOUCHERDETAIL_LINK_TAFSILI>, CancellationToken>((h, d, l, _) => _staged.Add((h, d, l)));
        _heads.Setup(h => h.GetNextDocNumAsync(Vahed, It.IsAny<string?>(), It.IsAny<CancellationToken>())).ReturnsAsync("000009");
    }

    private void Balances(string year, bool includeClosing, params YearEndBalance[] rows)
        => _repo.Setup(r => r.GetBalancesAsync(Vahed, year, It.IsAny<IReadOnlyCollection<char>>(), includeClosing, It.IsAny<CancellationToken>()))
            .ReturnsAsync(rows);

    private static YearEndBalance B(string code, decimal net, params YearEndTafsili[] tafsilis)
        => new(Guid.NewGuid(), code, "حساب " + code, tafsilis, net);

    private Task<YearEndPreviewDto> Preview(string kind, string year)
        => new GetYearEndPreviewHandler(_repo.Object).Handle(new GetYearEndPreviewQuery(kind, year) { VahedCode = Vahed }, default);

    private Task<YearEndIssueResultDto> Issue(string kind, string year)
        => new IssueYearEndVouchersHandler(_repo.Object, _heads.Object, _user.Object, _uow.Object)
            .Handle(new IssueYearEndVouchersCommand(kind, year) { VahedCode = Vahed }, default);

    [Fact]
    public async Task Unknown_unit_type_is_business_error()
    {
        _repo.Setup(r => r.GetVahedTypeIdAsync(Vahed, It.IsAny<CancellationToken>())).ReturnsAsync((Guid?)null);
        await Assert.ThrowsAsync<BusinessRuleException>(() => Preview(YearEndKinds.Opening, "1405"));
    }

    [Fact]
    public async Task Opening_reads_previous_year_including_closing_and_dates_first_day()
    {
        Balances("1404", includeClosing: true, B("101", 100), B("201", -100));

        var p = await Preview(YearEndKinds.Opening, "1405");

        Assert.Equal("1404", p.SourceYear);
        Assert.Equal("1405", p.TargetYear);
        Assert.Equal("14050101", p.DateDoc);
        Assert.Equal(2, p.Vouchers.Count);
        Assert.Empty(p.Problems);
        Assert.All(p.Vouchers, v => Assert.Equal(v.TotalDebtor, v.TotalCreditor));
    }

    [Fact]
    public async Task Opening_without_interface_account_is_problem()
    {
        _repo.Setup(r => r.GetInterfaceAccountAsync(InterfaceType.OpenVoucher, It.IsAny<CancellationToken>())).ReturnsAsync((YearEndAccountRef?)null);
        var p = await Preview(YearEndKinds.Opening, "1405");
        Assert.Contains(p.Problems, x => x.Contains("رابط افتتاحیه"));
    }

    [Fact]
    public async Task Opening_already_issued_is_problem()
    {
        Balances("1404", includeClosing: true, B("101", 100));
        _repo.Setup(r => r.GetDocNumsWithAccountAsync(Vahed, "1405", Iface.Id, It.IsAny<CancellationToken>())).ReturnsAsync(new[] { "000001" });
        var p = await Preview(YearEndKinds.Opening, "1405");
        Assert.Contains(p.Problems, x => x.Contains("000001"));
    }

    [Fact]
    public async Task Opening_with_nothing_to_move_is_problem()
    {
        var p = await Preview(YearEndKinds.Opening, "1405");
        Assert.Contains(p.Problems, x => x.Contains("ماندهٔ قابل انتقالی"));
    }

    [Fact]
    public async Task Opening_warns_about_missing_closing_and_drafts()
    {
        Balances("1404", includeClosing: true, B("101", 100));
        _repo.Setup(r => r.CountDraftVouchersAsync(Vahed, "1404", It.IsAny<CancellationToken>())).ReturnsAsync(3);

        var p = await Preview(YearEndKinds.Opening, "1405");

        Assert.Contains(p.Warnings, w => w.Contains("اختتامیهٔ سال 1404"));
        Assert.Contains(p.Warnings, w => w.StartsWith("3 سند"));
    }

    [Fact]
    public async Task Closing_dates_last_day_and_reports_unmapped_accounts()
    {
        Balances("1404", includeClosing: false, B("601", 50));
        var p = await Preview(YearEndKinds.Closing, "1404");
        Assert.Equal("14041229", p.DateDoc);
        Assert.Contains(p.Problems, x => x.Contains("601"));
    }

    [Fact]
    public async Task Closing_already_issued_is_problem()
    {
        var b = B("601", 50);
        Balances("1404", includeClosing: false, b);
        _repo.Setup(r => r.GetClosingRabetMapAsync(VahedType, "1404", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<Guid, YearEndAccountRef> { [b.AccountId] = Rabet });
        _repo.Setup(r => r.GetClosingDocNumsAsync(Vahed, "1404", It.IsAny<CancellationToken>())).ReturnsAsync(new[] { "000100" });

        var p = await Preview(YearEndKinds.Closing, "1404");

        Assert.Contains(p.Problems, x => x.Contains("000100"));
    }

    [Fact]
    public async Task Closing_with_no_balances_is_problem()
    {
        var p = await Preview(YearEndKinds.Closing, "1404");
        Assert.Contains(p.Problems, x => x.Contains("ماندهٔ بازی"));
        Assert.Empty(p.Vouchers);
    }

    [Fact]
    public async Task Issue_with_problems_throws_and_stages_nothing()
    {
        await Assert.ThrowsAsync<BusinessRuleException>(() => Issue(YearEndKinds.Closing, "1404"));
        Assert.Empty(_staged);
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Issue_opening_numbers_vouchers_consecutively_as_draft_automatic()
    {
        var t = new YearEndTafsili(Guid.NewGuid(), "1", Guid.NewGuid(), "T1", "تفصیلی");
        Balances("1404", includeClosing: true, B("101", 100, t), B("201", -40), B("301", 5));

        var result = await Issue(YearEndKinds.Opening, "1405");

        Assert.Equal(new[] { "000009", "000010", "000011" }, result.DocNums);
        Assert.Equal(3, _staged.Count);
        Assert.All(_staged, s =>
        {
            Assert.Equal(DocLife.Draft, s.Head.DOCLIFE);
            Assert.True(s.Head.ISAUTOMATIC);
            Assert.Null(s.Head.FLAG_STATE);
            Assert.Equal("1405", s.Head.YEAR);
            Assert.Equal("14050101", s.Head.DATE_DOC);
            Assert.Equal(s.Details.Sum(d => d.DEBTOR), s.Details.Sum(d => d.CREDITOR));
            Assert.All(s.Details, d => Assert.Equal(s.Head.ID, d.VOUCHERSHEAD_ID));
        });

        var first = _staged[0];
        Assert.Equal(new int?[] { 1, 2 }, first.Details.Select(d => d.RADIF));
        var link = Assert.Single(first.Links);
        Assert.Equal(t.TafsiliId, link.TAFSILI_ID);
        Assert.Equal(first.Details[0].ID, link.VOUCHERSDETAIL_ID);
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Issue_closing_sets_closing_flag_on_single_voucher()
    {
        var revenue = B("601", -300);
        var expense = B("801", 120);
        Balances("1404", includeClosing: false, revenue, expense);
        _repo.Setup(r => r.GetClosingRabetMapAsync(VahedType, "1404", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<Guid, YearEndAccountRef> { [revenue.AccountId] = Rabet, [expense.AccountId] = Rabet });

        var result = await Issue(YearEndKinds.Closing, "1404");

        Assert.Equal("000009", Assert.Single(result.DocNums));
        var s = Assert.Single(_staged);
        Assert.Equal(YearEndKinds.ClosingFlag, s.Head.FLAG_STATE);
        Assert.Equal("14041229", s.Head.DATE_DOC);
        Assert.Equal(3, s.Details.Count);
        var rabetLine = s.Details.Single(d => d.ACCOUNT_ID == Rabet.Id);
        Assert.Equal(180m, rabetLine.CREDITOR);
    }

    [Theory]
    [InlineData("other", "1404", false)]
    [InlineData("opening", "14", false)]
    [InlineData("closing", "1404", true)]
    public void Validators_check_kind_and_year(string kind, string year, bool valid)
    {
        Assert.Equal(valid, new GetYearEndPreviewValidator().Validate(new GetYearEndPreviewQuery(kind, year)).IsValid);
        Assert.Equal(valid, new IssueYearEndVouchersValidator().Validate(new IssueYearEndVouchersCommand(kind, year)).IsValid);
    }
}
