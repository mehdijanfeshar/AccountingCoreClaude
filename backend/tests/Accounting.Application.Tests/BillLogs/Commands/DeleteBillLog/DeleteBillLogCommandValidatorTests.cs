using Accounting.Application.BillLogs.Commands.DeleteBillLog;

namespace Accounting.Application.Tests.BillLogs.Commands.DeleteBillLog;

public sealed class DeleteBillLogCommandValidatorTests
{
    private readonly DeleteBillLogCommandValidator _validator = new();

    [Fact]
    public void Validate_NonEmptyId_Passes()
    {
        var result = _validator.Validate(new DeleteBillLogCommand(Guid.NewGuid()));

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validate_EmptyId_Fails()
    {
        var result = _validator.Validate(new DeleteBillLogCommand(Guid.Empty));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(DeleteBillLogCommand.Id));
    }
}
