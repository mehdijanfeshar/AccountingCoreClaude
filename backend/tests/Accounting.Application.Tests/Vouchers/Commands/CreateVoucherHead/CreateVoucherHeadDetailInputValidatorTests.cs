using Accounting.Application.Vouchers.Commands.CreateVoucherHead;

namespace Accounting.Application.Tests.Vouchers.Commands.CreateVoucherHead;

public sealed class CreateVoucherHeadDetailInputValidatorTests
{
    private readonly CreateVoucherHeadDetailInputValidator _validator = new();

    private static CreateVoucherHeadDetailInput ValidInput() => new(
        AccountId: Guid.NewGuid(),
        ReceiptId: null,
        CheckId: null,
        LowLevelCodeId: null,
        EtebarId: null,
        Description: "line",
        Radif: 1,
        Debtor: 1000m,
        Creditor: null);

    [Fact]
    public void Validate_ValidInput_Passes()
    {
        var result = _validator.Validate(ValidInput());

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validate_AllFksNull_Passes()
    {
        var input = ValidInput() with
        {
            AccountId = null,
            ReceiptId = null,
            CheckId = null,
            LowLevelCodeId = null,
            EtebarId = null,
        };

        var result = _validator.Validate(input);

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validate_DescriptionAtMaxLength_Passes()
    {
        var input = ValidInput() with { Description = new string('a', 200) };

        var result = _validator.Validate(input);

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validate_DescriptionOverMaxLength_Fails()
    {
        var input = ValidInput() with { Description = new string('a', 201) };

        var result = _validator.Validate(input);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(CreateVoucherHeadDetailInput.Description));
    }

    [Fact]
    public void Validate_DescriptionNull_Passes()
    {
        var input = ValidInput() with { Description = null };

        var result = _validator.Validate(input);

        Assert.True(result.IsValid);
    }
}

/// <summary>
/// Covers <see cref="CreateVoucherHeadCommandValidator"/>'s <c>RuleForEach(x =&gt; x.InitialDetails)</c>
/// wiring specifically — i.e. the parent validator's null/empty/populated behaviour for the new
/// composite-create field, kept separate from <see cref="CreateVoucherHeadCommandValidatorTests"/>
/// (which pins the pre-existing header-only fields) so the new coverage is easy to find.
/// </summary>
public sealed class CreateVoucherHeadCommandValidatorInitialDetailsTests
{
    private readonly CreateVoucherHeadCommandValidator _validator = new();

    private static CreateVoucherHeadCommand ValidCommand(
        IReadOnlyList<CreateVoucherHeadDetailInput>? initialDetails = null) => new(
        DocNum: "000001",
        DateDoc: "14050101",
        DocLife: true,
        HeadDesc: "سند افتتاحیه",
        Apendix: null,
        SystemTypeId: null,
        FlagState: null,
        VahedCode: "0001",
        Year: "1405",
        IsAutomatic: false,
        SndVahedCode: null,
        ParentHeadId: null,
        AttachFileName: null,
        AtfNum: null,
        InitialDetails: initialDetails);

    [Fact]
    public void Validate_InitialDetailsNull_IsNoOp_CommandStillValid()
    {
        var result = _validator.Validate(ValidCommand(initialDetails: null));

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validate_InitialDetailsEmpty_Passes()
    {
        var result = _validator.Validate(ValidCommand(initialDetails: Array.Empty<CreateVoucherHeadDetailInput>()));

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validate_InitialDetailsWithValidEntries_Passes()
    {
        var details = new[]
        {
            new CreateVoucherHeadDetailInput(Guid.NewGuid(), null, null, null, null, "line 1", 1, 1000m, null),
            new CreateVoucherHeadDetailInput(Guid.NewGuid(), null, null, null, null, "line 2", 2, null, 1000m),
        };

        var result = _validator.Validate(ValidCommand(details));

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validate_InitialDetailsWithOneInvalidEntry_Fails()
    {
        var details = new[]
        {
            new CreateVoucherHeadDetailInput(Guid.NewGuid(), null, null, null, null, "ok line", 1, 1000m, null),
            new CreateVoucherHeadDetailInput(Guid.NewGuid(), null, null, null, null, new string('a', 201), 2, null, 1000m),
        };

        var result = _validator.Validate(ValidCommand(details));

        Assert.False(result.IsValid);
    }
}
