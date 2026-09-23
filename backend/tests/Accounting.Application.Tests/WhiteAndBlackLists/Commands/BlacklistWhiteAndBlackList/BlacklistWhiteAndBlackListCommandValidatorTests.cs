using Accounting.Application.WhiteAndBlackLists.Commands.BlacklistWhiteAndBlackList;

namespace Accounting.Application.Tests.WhiteAndBlackLists.Commands.BlacklistWhiteAndBlackList;

public sealed class BlacklistWhiteAndBlackListCommandValidatorTests
{
    private static readonly BlacklistWhiteAndBlackListCommandValidator Validator = new();

    [Fact]
    public void Validate_NonEmptyId_Passes()
    {
        Assert.True(Validator.Validate(new BlacklistWhiteAndBlackListCommand(Guid.NewGuid())).IsValid);
    }

    [Fact]
    public void Validate_EmptyId_Fails()
    {
        var result = Validator.Validate(new BlacklistWhiteAndBlackListCommand(Guid.Empty));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(BlacklistWhiteAndBlackListCommand.Id));
    }
}
