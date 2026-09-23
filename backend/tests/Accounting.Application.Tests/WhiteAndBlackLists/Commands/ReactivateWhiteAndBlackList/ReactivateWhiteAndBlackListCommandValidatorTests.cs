using Accounting.Application.WhiteAndBlackLists.Commands.ReactivateWhiteAndBlackList;
using Accounting.Domain.ValueObjects;

namespace Accounting.Application.Tests.WhiteAndBlackLists.Commands.ReactivateWhiteAndBlackList;

public sealed class ReactivateWhiteAndBlackListCommandValidatorTests
{
    private static readonly ReactivateWhiteAndBlackListCommandValidator Validator = new();

    private static ReactivateWhiteAndBlackListCommand ValidCommand() => new(
        Id: Guid.NewGuid(),
        State: WhiteBlackListState.Allowed,
        FromDate: "14040101",
        ToDate: "14041229");

    [Fact]
    public void Validate_ValidCommand_Passes()
    {
        Assert.True(Validator.Validate(ValidCommand()).IsValid);
    }

    [Fact]
    public void Validate_EmptyId_Fails()
    {
        Assert.False(Validator.Validate(ValidCommand() with { Id = Guid.Empty }).IsValid);
    }

    /// <summary>
    /// Reactivating INTO the blacklisted state is a contradiction in terms; «غیرفعال‌سازی» is the
    /// operation for that. Enforced on the server, not only by the dialog's radio group.
    /// </summary>
    [Fact]
    public void Validate_BlacklistedState_Fails()
    {
        var result = Validator.Validate(ValidCommand() with { State = WhiteBlackListState.Blacklisted });

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(ReactivateWhiteAndBlackListCommand.State));
    }

    [Theory]
    [InlineData(WhiteBlackListState.Allowed)]
    [InlineData(WhiteBlackListState.SystemOnly)]
    public void Validate_AllowedAndSystemOnlyStates_Pass(WhiteBlackListState state)
    {
        Assert.True(Validator.Validate(ValidCommand() with { State = state }).IsValid);
    }

    [Fact]
    public void Validate_StateOutsideEnum_Fails()
    {
        Assert.False(Validator.Validate(ValidCommand() with { State = (WhiteBlackListState)0 }).IsValid);
    }

    [Theory]
    [InlineData("1404010")]
    [InlineData("1404/01/01")]
    public void Validate_MalformedDate_Fails(string date)
    {
        Assert.False(Validator.Validate(ValidCommand() with { FromDate = date }).IsValid);
        Assert.False(Validator.Validate(ValidCommand() with { ToDate = date }).IsValid);
    }

    [Fact]
    public void Validate_FromDateAfterToDate_Fails()
    {
        Assert.False(Validator.Validate(ValidCommand() with { FromDate = "14041229", ToDate = "14040101" }).IsValid);
    }

    [Fact]
    public void Validate_NullDates_Pass()
    {
        Assert.True(Validator.Validate(ValidCommand() with { FromDate = null, ToDate = null }).IsValid);
    }
}
