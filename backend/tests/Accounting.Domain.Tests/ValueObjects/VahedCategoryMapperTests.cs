using Accounting.Domain.ValueObjects;

namespace Accounting.Domain.Tests.ValueObjects;

/// <summary>
/// Pure unit tests for <see cref="VahedCategoryMapper.FromTypeCode"/> — phase 20-b Rule B
/// caller-category derivation. See that method's XML doc for the exact TYPECODE tables copied
/// from the reference project.
/// </summary>
public sealed class VahedCategoryMapperTests
{
    [Theory]
    [InlineData("1")] // EdareKol
    [InlineData("9")] // Shob
    public void FromTypeCode_InsuranceTypeCodes_ReturnInsurance(string typeCode)
    {
        Assert.Equal(VahedCategory.Insurance, VahedCategoryMapper.FromTypeCode(typeCode));
    }

    [Theory]
    [InlineData("2")]
    [InlineData("3")]
    [InlineData("4")]
    [InlineData("5")]
    [InlineData("6")]
    [InlineData("7")]
    [InlineData("8")]
    [InlineData("15")]
    [InlineData("16")]
    public void FromTypeCode_TreatmentTypeCodes_ReturnTreatment(string typeCode)
    {
        Assert.Equal(VahedCategory.Treatment, VahedCategoryMapper.FromTypeCode(typeCode));
    }

    [Fact]
    public void FromTypeCode_UnknownNumericTypeCode_ReturnsAll()
    {
        Assert.Equal(VahedCategory.All, VahedCategoryMapper.FromTypeCode("999"));
    }

    [Fact]
    public void FromTypeCode_NonNumericTypeCode_ReturnsAll_DoesNotThrow()
    {
        Assert.Equal(VahedCategory.All, VahedCategoryMapper.FromTypeCode("not-a-number"));
    }

    [Fact]
    public void FromTypeCode_Null_ReturnsAll_DoesNotThrow()
    {
        Assert.Equal(VahedCategory.All, VahedCategoryMapper.FromTypeCode(null));
    }

    [Fact]
    public void FromTypeCode_EmptyString_ReturnsAll_DoesNotThrow()
    {
        Assert.Equal(VahedCategory.All, VahedCategoryMapper.FromTypeCode(string.Empty));
    }
}
