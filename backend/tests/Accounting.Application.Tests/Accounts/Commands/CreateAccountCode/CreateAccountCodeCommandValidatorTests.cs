using Accounting.Application.Accounts.Commands.CreateAccountCode;

namespace Accounting.Application.Tests.Accounts.Commands.CreateAccountCode;

public sealed class CreateAccountCodeCommandValidatorTests
{
    private readonly CreateAccountCodeCommandValidator _validator = new();

    private static CreateAccountCodeCommand ValidCommand() => new(
        TypeCode: true,
        ParentId: null,
        AccCode: "100100",
        AccCodeName: "بانک ملی",
        TypeActivity: true,
        SourceAndConsumeId: null,
        IdentyGroupsId: null,
        TypeAccCode: true,
        AddUserId: "user1",
        MoInforClose: null,
        TypeAction: null);

    [Fact]
    public void Validate_ValidCommand_Passes()
    {
        var result = _validator.Validate(ValidCommand());

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validate_EmptyAccCode_Fails()
    {
        var command = ValidCommand() with { AccCode = string.Empty };

        var result = _validator.Validate(command);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(CreateAccountCodeCommand.AccCode));
    }

    [Fact]
    public void Validate_AccCodeOverMaxLength_Fails()
    {
        var command = ValidCommand() with { AccCode = "1001001" }; // 7 chars, max is 6

        var result = _validator.Validate(command);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(CreateAccountCodeCommand.AccCode));
    }

    [Fact]
    public void Validate_AccCodeAtMaxLength_Passes()
    {
        var command = ValidCommand() with { AccCode = "100100" }; // 6 chars, at max

        var result = _validator.Validate(command);

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validate_EmptyAccCodeName_Fails()
    {
        var command = ValidCommand() with { AccCodeName = string.Empty };

        var result = _validator.Validate(command);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(CreateAccountCodeCommand.AccCodeName));
    }

    [Fact]
    public void Validate_AccCodeNameOverMaxLength_Fails()
    {
        var command = ValidCommand() with { AccCodeName = new string('a', 201) };

        var result = _validator.Validate(command);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(CreateAccountCodeCommand.AccCodeName));
    }

    [Fact]
    public void Validate_AddUserIdOverMaxLength_Fails()
    {
        var command = ValidCommand() with { AddUserId = new string('a', 11) };

        var result = _validator.Validate(command);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(CreateAccountCodeCommand.AddUserId));
    }

    [Fact]
    public void Validate_AddUserIdNull_Passes()
    {
        var command = ValidCommand() with { AddUserId = null };

        var result = _validator.Validate(command);

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validate_MoInforCloseOverMaxLength_Fails()
    {
        var command = ValidCommand() with { MoInforClose = new string('a', 7) };

        var result = _validator.Validate(command);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(CreateAccountCodeCommand.MoInforClose));
    }

    [Fact]
    public void Validate_MoInforCloseNull_Passes()
    {
        var command = ValidCommand() with { MoInforClose = null };

        var result = _validator.Validate(command);

        Assert.True(result.IsValid);
    }
}
