using Accounting.Application.ChequesIncorrents.Queries.GetChequesIncorrentById;

namespace Accounting.Application.Tests.ChequesIncorrents.Queries.GetChequesIncorrentById;

public sealed class GetChequesIncorrentByIdQueryValidatorTests
{
    private readonly GetChequesIncorrentByIdQueryValidator _validator = new();

    [Fact]
    public void Validate_NonEmptyGuid_Passes()
    {
        var result = _validator.Validate(new GetChequesIncorrentByIdQuery(Guid.NewGuid()));

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validate_EmptyGuid_Fails()
    {
        var result = _validator.Validate(new GetChequesIncorrentByIdQuery(Guid.Empty));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(GetChequesIncorrentByIdQuery.Id));
    }
}
