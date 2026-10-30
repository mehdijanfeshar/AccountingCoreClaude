using Accounting.Application.Common.Exceptions;
using Accounting.Application.Common.Interfaces;
using Accounting.Application.Reports.OpeningBalanceDiscrepancy;
using Accounting.Application.Vouchers.YearEnd;
using Accounting.Domain.ValueObjects;
using Moq;

namespace Accounting.Application.Tests.Reports.OpeningBalanceDiscrepancy;

/// <summary>
/// مغایرت افتتاحیه با ماندهٔ سال قبل: مقایسهٔ ردیف‌های واقعی سند افتتاحیهٔ سال جاری (به تفکیک معین/شناسه)
/// با ماندهٔ همان معین/شناسه در کل سال قبل؛ فقط نابرابری‌ها نمایش داده می‌شوند.
/// </summary>
public class GetOpeningBalanceDiscrepancyQueryHandlerTests
{
    private const string Vahed = "0042";
    private static readonly Guid IfaceId = Guid.NewGuid();
    private static readonly Guid AccountA = Guid.NewGuid();
    private static readonly Guid AccountB = Guid.NewGuid();

    private readonly Mock<IYearEndRepository> _yearEnd = new();
    private readonly Mock<IOpeningBalanceDiscrepancyReadRepository> _repo = new();

    public GetOpeningBalanceDiscrepancyQueryHandlerTests()
        => _yearEnd.Setup(y => y.GetInterfaceAccountAsync(InterfaceType.OpenVoucher, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new YearEndAccountRef(IfaceId, "900001", "رابط افتتاحیه"));

    private GetOpeningBalanceDiscrepancyQueryHandler Handler() => new(_yearEnd.Object, _repo.Object);

    private void Opening(params OpeningBalanceAccountLine[] lines)
        => _repo.Setup(r => r.GetOpeningLinesAsync(Vahed, "1404", IfaceId, It.IsAny<CancellationToken>())).ReturnsAsync(lines);

    private void Prior(params OpeningBalanceAccountLine[] lines)
        => _repo.Setup(r => r.GetYearBalancesByAttributeAsync(Vahed, "1403", It.IsAny<CancellationToken>())).ReturnsAsync(lines);

    private Task<IReadOnlyList<OpeningBalanceDiscrepancyRowDto>> Run()
        => Handler().Handle(new GetOpeningBalanceDiscrepancyQuery("1404") { VahedCode = Vahed }, default);

    [Fact]
    public async Task No_interface_account_is_a_business_error()
    {
        _yearEnd.Setup(y => y.GetInterfaceAccountAsync(InterfaceType.OpenVoucher, It.IsAny<CancellationToken>()))
            .ReturnsAsync((YearEndAccountRef?)null);
        await Assert.ThrowsAsync<BusinessRuleException>(Run);
    }

    [Fact]
    public async Task Reads_prior_year_balances_from_year_minus_one()
    {
        Opening();
        Prior();
        await Run();
        _repo.Verify(r => r.GetYearBalancesByAttributeAsync(Vahed, "1403", It.IsAny<CancellationToken>()));
    }

    [Fact]
    public async Task Matching_balance_is_not_a_discrepancy()
    {
        Opening(new OpeningBalanceAccountLine(AccountA, "500001", "حساب الف", null, 1000));
        Prior(new OpeningBalanceAccountLine(AccountA, "500001", "حساب الف", null, 1000));

        var rows = await Run();

        Assert.Empty(rows);
    }

    [Fact]
    public async Task Different_balance_is_reported_with_the_difference()
    {
        Opening(new OpeningBalanceAccountLine(AccountA, "500001", "حساب الف", null, 1000));
        Prior(new OpeningBalanceAccountLine(AccountA, "500001", "حساب الف", null, 800));

        var row = Assert.Single(await Run());

        Assert.Equal("500001", row.AccCode);
        Assert.Equal(1000m, row.OpeningAmount);
        Assert.Equal(800m, row.PriorYearBalance);
        Assert.Equal(200m, row.Difference);
    }

    [Fact]
    public async Task Missing_prior_year_balance_defaults_to_zero()
    {
        Opening(new OpeningBalanceAccountLine(AccountA, "500001", "حساب الف", null, 500));
        Prior();

        var row = Assert.Single(await Run());

        Assert.Equal(0m, row.PriorYearBalance);
        Assert.Equal(500m, row.Difference);
    }

    [Fact]
    public async Task Compares_per_identifier_for_identified_accounts()
    {
        Opening(
            new OpeningBalanceAccountLine(AccountA, "500001", "حساب شناسه‌دار", "101", 300),
            new OpeningBalanceAccountLine(AccountA, "500001", "حساب شناسه‌دار", "102", 150));
        Prior(
            new OpeningBalanceAccountLine(AccountA, "500001", "حساب شناسه‌دار", "101", 300),
            new OpeningBalanceAccountLine(AccountA, "500001", "حساب شناسه‌دار", "102", 999));

        var row = Assert.Single(await Run());

        Assert.Equal("102", row.Identifier);
        Assert.Equal(150m, row.OpeningAmount);
        Assert.Equal(999m, row.PriorYearBalance);
    }

    [Fact]
    public async Task Prior_balance_for_a_different_identifier_of_the_same_account_never_matches()
    {
        Opening(new OpeningBalanceAccountLine(AccountA, "500001", "حساب", "101", 300));
        Prior(new OpeningBalanceAccountLine(AccountA, "500001", "حساب", "999", 300));

        var row = Assert.Single(await Run());
        Assert.Equal(0m, row.PriorYearBalance);
    }

    [Fact]
    public async Task Results_ordered_by_account_code_then_identifier()
    {
        Opening(
            new OpeningBalanceAccountLine(AccountB, "600000", "ب", null, 10),
            new OpeningBalanceAccountLine(AccountA, "500001", "الف", "2", 10),
            new OpeningBalanceAccountLine(AccountA, "500001", "الف", "1", 10));
        Prior();

        var rows = await Run();

        Assert.Equal(new[] { ("500001", "1"), ("500001", "2"), ("600000", (string?)null) },
            rows.Select(r => (r.AccCode, r.Identifier)));
    }
}
