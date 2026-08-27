using Accounting.Application.BillLogs.Queries.GetBillLogs;

namespace Accounting.Application.Tests.BillLogs.Queries.GetBillLogs;

public sealed class GetBillLogsQueryValidatorTests
{
    private readonly GetBillLogsQueryValidator _validator = new();

    [Fact]
    public void Validate_ValidQuery_Passes()
    {
        var result = _validator.Validate(new GetBillLogsQuery(PageNumber: 1, PageSize: 20));

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validate_PageNumberLessThanOne_Fails()
    {
        var result = _validator.Validate(new GetBillLogsQuery(PageNumber: 0, PageSize: 20));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(GetBillLogsQuery.PageNumber));
    }

    [Fact]
    public void Validate_PageSizeAboveMax_Fails()
    {
        var result = _validator.Validate(new GetBillLogsQuery(PageNumber: 1, PageSize: GetBillLogsQueryValidator.MaxPageSize + 1));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(GetBillLogsQuery.PageSize));
    }

    [Fact]
    public void Validate_PageSizeAtMax_Passes()
    {
        var result = _validator.Validate(new GetBillLogsQuery(PageNumber: 1, PageSize: GetBillLogsQueryValidator.MaxPageSize));

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validate_PageSizeZero_Fails()
    {
        var result = _validator.Validate(new GetBillLogsQuery(PageNumber: 1, PageSize: 0));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(GetBillLogsQuery.PageSize));
    }

    [Fact]
    public void Validate_PageNumberAboveMax_Fails()
    {
        var result = _validator.Validate(new GetBillLogsQuery(PageNumber: 10_737_420, PageSize: 200));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(GetBillLogsQuery.PageNumber));
    }

    [Fact]
    public void Validate_PageNumberAtMax_Passes()
    {
        var result = _validator.Validate(new GetBillLogsQuery(PageNumber: GetBillLogsQueryValidator.MaxPageNumber, PageSize: 20));

        Assert.True(result.IsValid);
    }
}
