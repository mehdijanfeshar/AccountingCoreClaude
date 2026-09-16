using Accounting.Application.Accounts.Commands.UpdateAccountCode;
using Accounting.Domain.ValueObjects;

namespace Accounting.Application.Tests.Accounts.Commands.UpdateAccountCode;

public sealed class UpdateAccountCodeCommandValidatorTests
{
    private readonly UpdateAccountCodeCommandValidator _validator = new();

    private static UpdateAccountCodeCommand ValidCommand() => new(
        Id: Guid.NewGuid(),
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
    public void Validate_EmptyId_Fails()
    {
        var command = ValidCommand() with { Id = Guid.Empty };

        var result = _validator.Validate(command);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(UpdateAccountCodeCommand.Id));
    }

    [Fact]
    public void Validate_ParentIdEqualsOwnId_Fails()
    {
        var id = Guid.NewGuid();
        var command = ValidCommand() with { Id = id, ParentId = id };

        var result = _validator.Validate(command);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == "ParentId");
    }

    [Fact]
    public void Validate_ParentIdDifferentFromOwnId_Passes()
    {
        var command = ValidCommand() with { Id = Guid.NewGuid(), ParentId = Guid.NewGuid() };

        var result = _validator.Validate(command);

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validate_EmptyAccCode_Fails()
    {
        var command = ValidCommand() with { AccCode = string.Empty };

        var result = _validator.Validate(command);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(UpdateAccountCodeCommand.AccCode));
    }

    [Fact]
    public void Validate_AccCodeOverMaxLength_Fails()
    {
        var command = ValidCommand() with { AccCode = "1001001" }; // 7 chars, max is 6

        var result = _validator.Validate(command);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(UpdateAccountCodeCommand.AccCode));
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
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(UpdateAccountCodeCommand.AccCodeName));
    }

    [Fact]
    public void Validate_AccCodeNameOverMaxLength_Fails()
    {
        var command = ValidCommand() with { AccCodeName = new string('a', 201) };

        var result = _validator.Validate(command);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(UpdateAccountCodeCommand.AccCodeName));
    }

    [Fact]
    public void Validate_AccCodeNameAtMaxLength_Passes()
    {
        var command = ValidCommand() with { AccCodeName = new string('a', 200) };

        var result = _validator.Validate(command);

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validate_MoInforCloseOverMaxLength_Fails()
    {
        var command = ValidCommand() with { MoInforClose = new string('a', 7) };

        var result = _validator.Validate(command);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(UpdateAccountCodeCommand.MoInforClose));
    }

    [Fact]
    public void Validate_MoInforCloseAtMaxLength_Passes()
    {
        var command = ValidCommand() with { MoInforClose = new string('a', 6) };

        var result = _validator.Validate(command);

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validate_MoInforCloseNull_Passes()
    {
        var command = ValidCommand() with { MoInforClose = null };

        var result = _validator.Validate(command);

        Assert.True(result.IsValid);
    }

    // --- TYPECODE / TYPEACTIVITY / TYPEACCCODE / TYPEACTION enum coverage --------------------
    // These rules are dead code at runtime (see class-level XML doc — ValidationBehavior never
    // runs for this void command), but the validator class itself is still exercised directly
    // here, matching the precedent for every other rule in this test class.

    [Fact]
    public void Validate_TypeCodeOutOfRange_Fails()
    {
        var command = ValidCommand() with { TypeCode = (TypeCodes)99 };

        var result = _validator.Validate(command);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(UpdateAccountCodeCommand.TypeCode));
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
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(UpdateAccountCodeCommand.TypeActivity));
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
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(UpdateAccountCodeCommand.TypeAccCode));
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
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(UpdateAccountCodeCommand.TypeAction));
    }

    [Fact]
    public void Validate_TypeActionNull_Passes()
    {
        var command = ValidCommand() with { TypeAction = null };

        var result = _validator.Validate(command);

        Assert.True(result.IsValid);
    }
}
