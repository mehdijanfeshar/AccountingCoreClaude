using Accounting.Domain.ValueObjects;
using Accounting.Application.IdentitySubGroups.Queries.GetIdentitySubGroups;

namespace Accounting.Application.Tests.IdentitySubGroups.Queries.GetIdentitySubGroups;

public sealed class GetIdentitySubGroupsQueryValidatorTests
{
    private readonly GetIdentitySubGroupsQueryValidator _validator = new();

    [Fact]
    public void Validate_ValidQuery_Passes()
    {
        var result = _validator.Validate(new GetIdentitySubGroupsQuery(PageNumber: 1, PageSize: 20));

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validate_PageNumberLessThanOne_Fails()
    {
        var result = _validator.Validate(new GetIdentitySubGroupsQuery(PageNumber: 0, PageSize: 20));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(GetIdentitySubGroupsQuery.PageNumber));
    }

    [Fact]
    public void Validate_PageSizeAboveMax_Fails()
    {
        var result = _validator.Validate(new GetIdentitySubGroupsQuery(PageNumber: 1, PageSize: GetIdentitySubGroupsQueryValidator.MaxPageSize + 1));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(GetIdentitySubGroupsQuery.PageSize));
    }

    [Fact]
    public void Validate_PageSizeAtMax_Passes()
    {
        var result = _validator.Validate(new GetIdentitySubGroupsQuery(PageNumber: 1, PageSize: GetIdentitySubGroupsQueryValidator.MaxPageSize));

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validate_PageSizeZero_Fails()
    {
        var result = _validator.Validate(new GetIdentitySubGroupsQuery(PageNumber: 1, PageSize: 0));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(GetIdentitySubGroupsQuery.PageSize));
    }

    [Fact]
    public void Validate_PageNumberAboveMax_Fails()
    {
        var result = _validator.Validate(new GetIdentitySubGroupsQuery(PageNumber: 10_737_420, PageSize: 200));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(GetIdentitySubGroupsQuery.PageNumber));
    }

    [Fact]
    public void Validate_PageNumberAtMax_Passes()
    {
        var result = _validator.Validate(new GetIdentitySubGroupsQuery(PageNumber: GetIdentitySubGroupsQueryValidator.MaxPageNumber, PageSize: 20));

        Assert.True(result.IsValid);
    }

    // --- Filters the شناسنامه entry form depends on -------------------------------------------

    private static GetIdentitySubGroupsQuery ValidQuery() => new(PageNumber: 1, PageSize: 20);

    /// <summary>
    /// Both filters are optional — omitting them is the plain "list every subgroup" case.
    /// </summary>
    [Fact]
    public void Validate_NoFiltersSupplied_Passes()
    {
        var result = _validator.Validate(ValidQuery());

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validate_BothFiltersSupplied_Passes()
    {
        var result = _validator.Validate(ValidQuery() with
        {
            IdentityGroupId = Guid.NewGuid(),
            Kind = IdentitySubGroupKind.Fixed,
        });

        Assert.True(result.IsValid);
    }

    /// <summary>
    /// An all-zero Guid is never a real group id — it almost always means the caller sent an
    /// unset value, so it is rejected rather than silently matching nothing.
    /// </summary>
    [Fact]
    public void Validate_EmptyIdentityGroupId_Fails()
    {
        var result = _validator.Validate(ValidQuery() with { IdentityGroupId = Guid.Empty });

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(GetIdentitySubGroupsQuery.IdentityGroupId));
    }

    [Fact]
    public void Validate_KindOutOfRange_Fails()
    {
        var result = _validator.Validate(ValidQuery() with { Kind = (IdentitySubGroupKind)99 });

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(GetIdentitySubGroupsQuery.Kind));
    }

    [Theory]
    [InlineData(IdentitySubGroupKind.Fixed)]
    [InlineData(IdentitySubGroupKind.Variable)]
    public void Validate_EveryRealKind_Passes(IdentitySubGroupKind kind)
    {
        var result = _validator.Validate(ValidQuery() with { Kind = kind });

        Assert.True(result.IsValid);
    }
}
