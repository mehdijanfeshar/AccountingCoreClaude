using Accounting.Application.Vouchers.Commands.Common;
using Accounting.Application.Vouchers.Commands.CreateVoucherDetail;
using Accounting.Application.Vouchers.Commands.UpdateVoucherDetail;

namespace Accounting.Application.Tests.Vouchers.Commands.Common;

/// <summary>
/// Covers <see cref="VoucherDetailTafsiliLinkInputValidator"/> and its wiring into both commands
/// that nest it. Both fields map to non-nullable Legacy columns, so <c>Guid.Empty</c> must be
/// rejected rather than written as a real-but-meaningless key.
/// </summary>
public sealed class VoucherDetailTafsiliLinkInputValidatorTests
{
    private static readonly VoucherDetailTafsiliLinkInputValidator Validator = new();

    [Fact]
    public void Validate_BothIdsPopulated_IsValid()
    {
        var result = Validator.Validate(new VoucherDetailTafsiliLinkInput(Guid.NewGuid(), Guid.NewGuid()));

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validate_EmptyTafsiliId_IsInvalid()
    {
        var result = Validator.Validate(new VoucherDetailTafsiliLinkInput(Guid.Empty, Guid.NewGuid()));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(VoucherDetailTafsiliLinkInput.TafsiliId));
    }

    [Fact]
    public void Validate_EmptyLevelId_IsInvalid()
    {
        var result = Validator.Validate(new VoucherDetailTafsiliLinkInput(Guid.NewGuid(), Guid.Empty));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(VoucherDetailTafsiliLinkInput.LevelId));
    }

    private static CreateVoucherDetailCommand CreateCommand(
        IReadOnlyList<VoucherDetailTafsiliLinkInput>? links) => new(
        VoucherHeadId: Guid.NewGuid(),
        AccountId: null,
        ReceiptId: null,
        CheckId: null,
        LowLevelCodeId: null,
        EtebarId: null,
        Description: "ردیف",
        Radif: 1,
        Debtor: 1m,
        Creditor: null,
        VahedCode: "0001",
        Year: "1405",
        TafsiliLinks: links);

    private static UpdateVoucherDetailCommand UpdateCommand(
        IReadOnlyList<VoucherDetailTafsiliLinkInput>? links) => new(
        Id: Guid.NewGuid(),
        AccountId: null,
        ReceiptId: null,
        CheckId: null,
        LowLevelCodeId: null,
        EtebarId: null,
        Description: "ردیف",
        Radif: 1,
        Debtor: 1m,
        Creditor: null,
        VahedCode: "0001",
        Year: "1405",
        TafsiliLinks: links);

    [Fact]
    public void CreateValidator_NestedInvalidLink_FailsTheWholeCommand()
    {
        var result = new CreateVoucherDetailCommandValidator().Validate(
            CreateCommand(new[] { new VoucherDetailTafsiliLinkInput(Guid.Empty, Guid.NewGuid()) }));

        Assert.False(result.IsValid);
    }

    [Fact]
    public void UpdateValidator_NestedInvalidLink_FailsTheWholeCommand()
    {
        var result = new UpdateVoucherDetailCommandValidator().Validate(
            UpdateCommand(new[] { new VoucherDetailTafsiliLinkInput(Guid.NewGuid(), Guid.Empty) }));

        Assert.False(result.IsValid);
    }

    /// <summary>
    /// Neither an absent nor an empty collection may be treated as a validation failure — on
    /// Create both mean "no تفصیلی", and on Update null means "leave them alone" while empty means
    /// "clear them". All four combinations must pass validation and be resolved by the handler.
    /// </summary>
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void BothValidators_NullOrEmptyLinkCollection_IsValid(bool useEmptyListInsteadOfNull)
    {
        var links = useEmptyListInsteadOfNull ? Array.Empty<VoucherDetailTafsiliLinkInput>() : null;

        Assert.True(new CreateVoucherDetailCommandValidator().Validate(CreateCommand(links)).IsValid);
        Assert.True(new UpdateVoucherDetailCommandValidator().Validate(UpdateCommand(links)).IsValid);
    }
}
