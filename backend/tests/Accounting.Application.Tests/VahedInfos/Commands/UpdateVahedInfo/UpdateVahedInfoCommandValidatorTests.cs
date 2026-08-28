using Accounting.Application.VahedInfos.Commands.UpdateVahedInfo;

namespace Accounting.Application.Tests.VahedInfos.Commands.UpdateVahedInfo;

public sealed class UpdateVahedInfoCommandValidatorTests
{
    private readonly UpdateVahedInfoCommandValidator _validator = new();

    private static UpdateVahedInfoCommand ValidCommand(Guid? id = null) => new(
        Id: id ?? Guid.NewGuid(),
        VahedCode: "0002",
        VahedName: "واحد جدید",
        CityId: Guid.NewGuid(),
        VahedTypeId: Guid.NewGuid(),
        ParentId: Guid.NewGuid());

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
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(UpdateVahedInfoCommand.Id));
    }

    [Fact]
    public void Validate_NullParentId_Passes()
    {
        var result = _validator.Validate(ValidCommand() with { ParentId = null });

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validate_ParentIdEqualsId_Fails_SelfReferenceGuard()
    {
        var id = Guid.NewGuid();
        var command = ValidCommand(id) with { ParentId = id };

        var result = _validator.Validate(command);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == "ParentId");
    }

    [Fact]
    public void Validate_ParentIdDifferentFromId_Passes_SelfReferenceGuard()
    {
        var id = Guid.NewGuid();
        var command = ValidCommand(id) with { ParentId = Guid.NewGuid() };

        var result = _validator.Validate(command);

        Assert.True(result.IsValid);
    }
}
