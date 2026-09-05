using Accounting.Application.Receipts.Queries.GetReceiptById;

namespace Accounting.Application.Tests.Receipts.Queries.GetReceiptById;

public sealed class GetReceiptByIdQueryValidatorTests
{
    private readonly GetReceiptByIdQueryValidator _validator = new();

    [Fact]
    public void Validate_NonEmptyGuid_Passes()
    {
        var result = _validator.Validate(new GetReceiptByIdQuery(Guid.NewGuid()));

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validate_EmptyGuid_Fails()
    {
        var result = _validator.Validate(new GetReceiptByIdQuery(Guid.Empty));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(GetReceiptByIdQuery.Id));
    }
}
