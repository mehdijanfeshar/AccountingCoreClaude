using Accounting.Application.ChequesIncorrents.Commands.DeleteChequesIncorrent;

namespace Accounting.Application.Tests.ChequesIncorrents.Commands.DeleteChequesIncorrent;

public sealed class DeleteChequesIncorrentCommandValidatorTests
{
    private readonly DeleteChequesIncorrentCommandValidator _validator = new();

    [Fact]
    public void Validate_NonEmptyId_Passes()
    {
        var result = _validator.Validate(new DeleteChequesIncorrentCommand(Guid.NewGuid()));

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validate_EmptyId_Fails()
    {
        var result = _validator.Validate(new DeleteChequesIncorrentCommand(Guid.Empty));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(DeleteChequesIncorrentCommand.Id));
    }
}
