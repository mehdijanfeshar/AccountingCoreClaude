using Accounting.Application.Accounts.Commands.CreateAccountCode;
using Accounting.Domain.ValueObjects;

namespace Accounting.Application.Tests.Accounts.Commands.CreateAccountCode;

public sealed class CreateAccountCodeCommandValidatorTests
{
    private readonly CreateAccountCodeCommandValidator _validator = new();

    private static CreateAccountCodeCommand ValidCommand() => new(
        TypeCode: TypeCodes.Moin,
        ParentId: null,
        AccCode: "100100",
        AccCodeName: "بانک ملی",
        TypeActivity: TypeActivity.Debit,
        SourceAndConsumeId: null,
        IdentyGroupsId: null,
        TypeAccCode: TypeAccCode.Permanent,
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

    // --- TYPECODE / TYPEACTIVITY / TYPEACCCODE / TYPEACTION enum coverage --------------------
    // Every one of these four enums starts at 1 (there is no 0 member), which is precisely why
    // bool? was the wrong CLR type for them: 0 (bool's implicit default/false) is out of range
    // for all four, and a bool could never represent TypeActivity.CreditFin (7).

    [Fact]
    public void Validate_TypeCodeZero_Fails()
    {
        var command = ValidCommand() with { TypeCode = (TypeCodes)0 };

        var result = _validator.Validate(command);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(CreateAccountCodeCommand.TypeCode));
    }

    [Fact]
    public void Validate_TypeCodeOutOfRange_Fails()
    {
        var command = ValidCommand() with { TypeCode = (TypeCodes)99 };

        var result = _validator.Validate(command);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(CreateAccountCodeCommand.TypeCode));
    }

    [Fact]
    public void Validate_TypeCodeNull_Passes()
    {
        var command = ValidCommand() with { TypeCode = null };

        var result = _validator.Validate(command);

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validate_TypeActivityOutOfRange_Fails()
    {
        var command = ValidCommand() with { TypeActivity = (TypeActivity)99 };

        var result = _validator.Validate(command);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(CreateAccountCodeCommand.TypeActivity));
    }

    [Fact]
    public void Validate_TypeActivityZero_Fails()
    {
        var command = ValidCommand() with { TypeActivity = (TypeActivity)0 };

        var result = _validator.Validate(command);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(CreateAccountCodeCommand.TypeActivity));
    }

    [Fact]
    public void Validate_TypeActivityNull_Passes()
    {
        var command = ValidCommand() with { TypeActivity = null };

        var result = _validator.Validate(command);

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validate_TypeActivityCreditFin_Passes()
    {
        // Boundary value 7 — outside the range the (wrong/stale) Oracle column comment implied
        // (1..3), and impossible to represent with the old bool? type.
        var command = ValidCommand() with { TypeActivity = TypeActivity.CreditFin };

        var result = _validator.Validate(command);

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validate_TypeAccCodeOutOfRange_Fails()
    {
        var command = ValidCommand() with { TypeAccCode = (TypeAccCode)99 };

        var result = _validator.Validate(command);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(CreateAccountCodeCommand.TypeAccCode));
    }

    [Fact]
    public void Validate_TypeAccCodeZero_Fails()
    {
        var command = ValidCommand() with { TypeAccCode = (TypeAccCode)0 };

        var result = _validator.Validate(command);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(CreateAccountCodeCommand.TypeAccCode));
    }

    [Fact]
    public void Validate_TypeAccCodeNull_Passes()
    {
        var command = ValidCommand() with { TypeAccCode = null };

        var result = _validator.Validate(command);

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validate_TypeActionOutOfRange_Fails()
    {
        var command = ValidCommand() with { TypeAction = (TypeAction)99 };

        var result = _validator.Validate(command);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(CreateAccountCodeCommand.TypeAction));
    }

    [Fact]
    public void Validate_TypeActionZero_Fails()
    {
        var command = ValidCommand() with { TypeAction = (TypeAction)0 };

        var result = _validator.Validate(command);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(CreateAccountCodeCommand.TypeAction));
    }

    [Fact]
    public void Validate_TypeActionNull_Passes()
    {
        var command = ValidCommand() with { TypeAction = null };

        var result = _validator.Validate(command);

        Assert.True(result.IsValid);
    }
}
