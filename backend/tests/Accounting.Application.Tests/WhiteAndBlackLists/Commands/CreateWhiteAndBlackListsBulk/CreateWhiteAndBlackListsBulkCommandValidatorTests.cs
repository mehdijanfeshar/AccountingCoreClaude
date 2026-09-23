using Accounting.Application.WhiteAndBlackLists.Commands.CreateWhiteAndBlackListsBulk;
using Accounting.Domain.ValueObjects;

namespace Accounting.Application.Tests.WhiteAndBlackLists.Commands.CreateWhiteAndBlackListsBulk;

public sealed class CreateWhiteAndBlackListsBulkCommandValidatorTests
{
    private static readonly CreateWhiteAndBlackListsBulkCommandValidator Validator = new();

    private static CreateWhiteAndBlackListsBulkCommand ValidCommand() => new(
        AccountCodeIds: [Guid.NewGuid()],
        VahedTypeIds: [Guid.NewGuid()],
        FromDate: "14040101",
        ToDate: "14041229",
        State: WhiteBlackListState.Allowed);

    [Fact]
    public void Validate_ValidCommand_Passes()
    {
        Assert.True(Validator.Validate(ValidCommand()).IsValid);
    }

    [Fact]
    public void Validate_EmptyAccountCodeIds_Fails()
    {
        var result = Validator.Validate(ValidCommand() with { AccountCodeIds = [] });

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(CreateWhiteAndBlackListsBulkCommand.AccountCodeIds));
    }

    [Fact]
    public void Validate_EmptyVahedTypeIds_Fails()
    {
        var result = Validator.Validate(ValidCommand() with { VahedTypeIds = [] });

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(CreateWhiteAndBlackListsBulkCommand.VahedTypeIds));
    }

    [Fact]
    public void Validate_EmptyGuidInsideAList_Fails()
    {
        Assert.False(Validator.Validate(ValidCommand() with { AccountCodeIds = [Guid.NewGuid(), Guid.Empty] }).IsValid);
        Assert.False(Validator.Validate(ValidCommand() with { VahedTypeIds = [Guid.Empty] }).IsValid);
    }

    /// <summary>
    /// Blacklisting is a transition applied to an existing row by the «غیرفعال‌سازی» action, never
    /// an initial state — the same rule the reference project's add dialog enforces by hiding the
    /// third radio option. Enforced on the server so calling the API directly cannot bypass it.
    /// </summary>
    [Fact]
    public void Validate_BlacklistedState_Fails()
    {
        var result = Validator.Validate(ValidCommand() with { State = WhiteBlackListState.Blacklisted });

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(CreateWhiteAndBlackListsBulkCommand.State));
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
        Assert.False(Validator.Validate(ValidCommand() with { State = (WhiteBlackListState)99 }).IsValid);
    }

    /// <summary>
    /// A date that is not exactly 8 digits would be compared lexicographically against
    /// different-width strings by the list filters and silently produce wrong pages, so it is
    /// rejected at the edge rather than stored.
    /// </summary>
    [Theory]
    [InlineData("1404010")]
    [InlineData("140401011")]
    [InlineData("1404/01/01")]
    [InlineData("abcdefgh")]
    public void Validate_MalformedDate_Fails(string date)
    {
        Assert.False(Validator.Validate(ValidCommand() with { FromDate = date }).IsValid);
        Assert.False(Validator.Validate(ValidCommand() with { ToDate = date }).IsValid);
    }

    [Fact]
    public void Validate_NullOrEmptyDates_Pass()
    {
        Assert.True(Validator.Validate(ValidCommand() with { FromDate = null, ToDate = null }).IsValid);
        Assert.True(Validator.Validate(ValidCommand() with { FromDate = string.Empty, ToDate = string.Empty }).IsValid);
    }

    [Fact]
    public void Validate_FromDateAfterToDate_Fails()
    {
        Assert.False(Validator.Validate(ValidCommand() with { FromDate = "14041229", ToDate = "14040101" }).IsValid);
    }

    [Fact]
    public void Validate_FromDateEqualToToDate_Passes()
    {
        Assert.True(Validator.Validate(ValidCommand() with { FromDate = "14040101", ToDate = "14040101" }).IsValid);
    }

    [Fact]
    public void Validate_ProductAboveTheCombinationCeiling_Fails()
    {
        var accounts = Enumerable.Range(0, 101).Select(_ => Guid.NewGuid()).ToArray();
        var vahedTypes = Enumerable.Range(0, 51).Select(_ => Guid.NewGuid()).ToArray();

        // 101 × 51 = 5151 > MaxCombinations.
        Assert.True(accounts.Length * vahedTypes.Length > CreateWhiteAndBlackListsBulkCommandValidator.MaxCombinations);
        Assert.False(Validator.Validate(ValidCommand() with
        {
            AccountCodeIds = accounts,
            VahedTypeIds = vahedTypes,
        }).IsValid);
    }

    /// <summary>
    /// The ceiling counts DISTINCT ids, matching what the handler actually stages — otherwise a
    /// caller could be refused for a request that expands to a handful of rows.
    /// </summary>
    [Fact]
    public void Validate_CountsDistinctIdsAgainstTheCeiling()
    {
        var account = Guid.NewGuid();
        var vahedType = Guid.NewGuid();
        var manyDuplicates = Enumerable.Range(0, 5_000).Select(_ => account).ToArray();

        Assert.True(Validator.Validate(ValidCommand() with
        {
            AccountCodeIds = manyDuplicates,
            VahedTypeIds = [vahedType],
        }).IsValid);
    }
}
