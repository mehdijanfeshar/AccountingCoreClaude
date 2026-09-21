using Accounting.Application.Common.Search;
using Accounting.Application.Reports.TrialBalance;

namespace Accounting.Application.Tests.Reports.TrialBalance;

/// <summary>
/// The generic <see cref="SearchParam"/> filter lets a caller name the field to filter on, which is
/// the one thing about it worth testing properly: a column name cannot be a bind variable, so if an
/// arbitrary name could reach the repository it would have to be concatenated into SQL. These tests
/// pin that it cannot.
/// </summary>
public sealed class TrialBalanceFilterValidatorTests
{
    private readonly TrialBalanceFilterValidator _validator = new();

    private static SearchParam Filter(string property, SearchOperator op = SearchOperator.LIKE, string value = "11") =>
        new() { Property = property, Operator = op, Value = value };

    [Theory]
    [InlineData("code")]
    [InlineData("description")]
    [InlineData("CODE")]
    [InlineData("Description")]
    public void AllowedFields_AreAccepted_CaseInsensitively(string property)
    {
        Assert.True(_validator.Validate(Filter(property)).IsValid);
    }

    [Theory]
    [InlineData("VAHEDCODE")]
    [InlineData("h.DOCLIFE")]
    [InlineData("a.ACCCODE")]
    [InlineData("1=1 OR 1")]
    [InlineData("")]
    public void AnythingElse_IsRejected(string property)
    {
        // Including a real column name: the allowlist is about what this report exposes, not only
        // about syntax. VAHEDCODE in particular must never become caller-filterable — the whole
        // point of phase 19/32 is that the unit is the server's decision.
        Assert.False(_validator.Validate(Filter(property)).IsValid);
    }

    [Fact]
    public void OperatorOutsideTheEnum_IsRejected()
    {
        // Casting past the enum is the only way to reach an operator the SQL mapping has no case
        // for, and that would throw deep inside the repository. Reject it at the edge instead.
        var filter = Filter("code");
        filter.Operator = (SearchOperator)99;

        Assert.False(_validator.Validate(filter).IsValid);
    }

    [Fact]
    public void EmptyValue_IsRejected()
    {
        // An empty LIKE term matches every row, so it is not a filter at all — a caller who sends
        // one almost certainly meant to send something and lost it on the way.
        Assert.False(_validator.Validate(Filter("code", value: string.Empty)).IsValid);
    }

    [Fact]
    public void OverlongValue_IsRejected()
    {
        var tooLong = new string('9', TrialBalanceFilterValidator.MaxValueLength + 1);

        Assert.False(_validator.Validate(Filter("code", value: tooLong)).IsValid);
    }
}
