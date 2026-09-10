using Accounting.Application.AccountCodes.Queries.GetTafsiliLevelItems;

namespace Accounting.Application.Tests.AccountCodes.Queries.GetTafsiliLevelItems;

public sealed class GetTafsiliLevelItemsQueryValidatorTests
{
    private readonly GetTafsiliLevelItemsQueryValidator _validator = new();

    private static GetTafsiliLevelItemsQuery ValidQuery(
        Guid? accountCodeId = null,
        Guid? levelId = null,
        string? search = null,
        int pageNumber = 1,
        int pageSize = 20) => new(
            accountCodeId ?? Guid.NewGuid(),
            levelId ?? Guid.NewGuid(),
            search,
            pageNumber,
            pageSize);

    [Fact]
    public void Validate_ValidQuery_Passes()
    {
        var result = _validator.Validate(ValidQuery());

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validate_EmptyAccountCodeId_Fails()
    {
        var result = _validator.Validate(ValidQuery(accountCodeId: Guid.Empty));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(GetTafsiliLevelItemsQuery.AccountCodeId));
    }

    [Fact]
    public void Validate_EmptyLevelId_Fails()
    {
        var result = _validator.Validate(ValidQuery(levelId: Guid.Empty));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(GetTafsiliLevelItemsQuery.LevelId));
    }

    [Fact]
    public void Validate_SearchLongerThan200Characters_Fails()
    {
        var result = _validator.Validate(ValidQuery(search: new string('ا', 201)));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(GetTafsiliLevelItemsQuery.Search));
    }

    [Fact]
    public void Validate_SearchAt200Characters_Passes()
    {
        var result = _validator.Validate(ValidQuery(search: new string('ا', 200)));

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validate_BlankSearch_Passes()
    {
        var result = _validator.Validate(ValidQuery(search: "   "));

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validate_NullSearch_Passes()
    {
        var result = _validator.Validate(ValidQuery(search: null));

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validate_PageNumberLessThanOne_Fails()
    {
        var result = _validator.Validate(ValidQuery(pageNumber: 0));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(GetTafsiliLevelItemsQuery.PageNumber));
    }

    [Fact]
    public void Validate_PageNumberAboveMax_Fails()
    {
        var result = _validator.Validate(ValidQuery(pageNumber: GetTafsiliLevelItemsQueryValidator.MaxPageNumber + 1));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(GetTafsiliLevelItemsQuery.PageNumber));
    }

    [Fact]
    public void Validate_PageNumberAtMax_Passes()
    {
        var result = _validator.Validate(ValidQuery(pageNumber: GetTafsiliLevelItemsQueryValidator.MaxPageNumber));

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validate_PageSizeZero_Fails()
    {
        var result = _validator.Validate(ValidQuery(pageSize: 0));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(GetTafsiliLevelItemsQuery.PageSize));
    }

    [Fact]
    public void Validate_PageSizeAboveMax_Fails()
    {
        var result = _validator.Validate(ValidQuery(pageSize: GetTafsiliLevelItemsQueryValidator.MaxPageSize + 1));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(GetTafsiliLevelItemsQuery.PageSize));
    }

    [Fact]
    public void Validate_PageSizeAtMax_Passes()
    {
        var result = _validator.Validate(ValidQuery(pageSize: GetTafsiliLevelItemsQueryValidator.MaxPageSize));

        Assert.True(result.IsValid);
    }
}
