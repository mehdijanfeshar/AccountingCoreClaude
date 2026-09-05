using Accounting.Application.CheckBooks.Commands.UpdateCheckBook;

namespace Accounting.Application.Tests.CheckBooks.Commands.UpdateCheckBook;

public sealed class UpdateCheckBookCommandValidatorTests
{
    private readonly UpdateCheckBookCommandValidator _validator = new();

    private static UpdateCheckBookCommand ValidCommand(Guid? id = null) => new(
        Id: id ?? Guid.NewGuid(),
        AccountId: Guid.NewGuid(),
        CheckBookTitle: "دسته چک به‌روزشده",
        CheckBookDate: "13990202",
        FromCheckNumber: "200000",
        ToCheckNumber: "200050",
        CheckTypeId: Guid.NewGuid(),
        VahedCode: "0002",
        CheckBookType: false,
        Serial: "SER0002");

    [Fact]
    public void Validate_ValidCommand_Passes()
    {
        var result = _validator.Validate(ValidCommand());

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validate_EmptyId_Fails()
    {
        var result = _validator.Validate(ValidCommand() with { Id = Guid.Empty });

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(UpdateCheckBookCommand.Id));
    }

    [Fact]
    public void Validate_EmptyAccountId_Fails()
    {
        var result = _validator.Validate(ValidCommand() with { AccountId = Guid.Empty });

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(UpdateCheckBookCommand.AccountId));
    }

    [Fact]
    public void Validate_NullOptionals_Passes()
    {
        var result = _validator.Validate(ValidCommand() with
        {
            CheckBookTitle = null,
            CheckTypeId = null,
            CheckBookType = null,
            Serial = null,
        });

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validate_CheckBookTitleTooLong_Fails()
    {
        var result = _validator.Validate(ValidCommand() with { CheckBookTitle = new string('a', 101) });

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(UpdateCheckBookCommand.CheckBookTitle));
    }

    [Fact]
    public void Validate_EmptyCheckBookDate_Fails()
    {
        var result = _validator.Validate(ValidCommand() with { CheckBookDate = string.Empty });

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(UpdateCheckBookCommand.CheckBookDate));
    }

    [Fact]
    public void Validate_CheckBookDateTooLong_Fails()
    {
        var result = _validator.Validate(ValidCommand() with { CheckBookDate = "139902021" });

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(UpdateCheckBookCommand.CheckBookDate));
    }

    [Fact]
    public void Validate_EmptyFromCheckNumber_Fails()
    {
        var result = _validator.Validate(ValidCommand() with { FromCheckNumber = string.Empty });

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(UpdateCheckBookCommand.FromCheckNumber));
    }

    [Fact]
    public void Validate_FromCheckNumberTooLong_Fails()
    {
        var result = _validator.Validate(ValidCommand() with { FromCheckNumber = new string('2', 15) });

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(UpdateCheckBookCommand.FromCheckNumber));
    }

    [Fact]
    public void Validate_EmptyToCheckNumber_Fails()
    {
        var result = _validator.Validate(ValidCommand() with { ToCheckNumber = string.Empty });

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(UpdateCheckBookCommand.ToCheckNumber));
    }

    [Fact]
    public void Validate_ToCheckNumberTooLong_Fails()
    {
        var result = _validator.Validate(ValidCommand() with { ToCheckNumber = new string('2', 15) });

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(UpdateCheckBookCommand.ToCheckNumber));
    }

    [Fact]
    public void Validate_EmptyVahedCode_Fails()
    {
        var result = _validator.Validate(ValidCommand() with { VahedCode = string.Empty });

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(UpdateCheckBookCommand.VahedCode));
    }

    [Fact]
    public void Validate_VahedCodeTooLong_Fails()
    {
        var result = _validator.Validate(ValidCommand() with { VahedCode = "00002" });

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(UpdateCheckBookCommand.VahedCode));
    }

    [Fact]
    public void Validate_SerialTooLong_Fails()
    {
        var result = _validator.Validate(ValidCommand() with { Serial = new string('a', 21) });

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(UpdateCheckBookCommand.Serial));
    }
}
