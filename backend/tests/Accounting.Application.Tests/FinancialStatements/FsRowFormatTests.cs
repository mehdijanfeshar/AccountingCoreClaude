using Accounting.Application.FinancialStatements;

namespace Accounting.Application.Tests.FinancialStatements;

/// <summary>سریال‌سازی قالب‌بندی ردیف به FORMAT_JSON.</summary>
public sealed class FsRowFormatTests
{
    [Fact]
    public void ToJson_NullOrDefault_ReturnsNull()
    {
        Assert.Null(FsRowFormat.ToJson(null));
        Assert.Null(FsRowFormat.ToJson(new FsRowFormat()));
        Assert.Null(FsRowFormat.ToJson(FsRowFormat.Default));
    }

    [Fact]
    public void ToJson_FromJson_RoundTrips()
    {
        var format = new FsRowFormat(Indent: 2, Bold: true, TopBorder: FsBorder.Single, BottomBorder: FsBorder.Double, HideIfZero: true, InnerColumn: true);

        var json = FsRowFormat.ToJson(format);

        Assert.NotNull(json);
        Assert.Contains("\"bold\":true", json);   // camelCase (Web defaults)
        Assert.Equal(format, FsRowFormat.FromJson(json));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("{not json")]
    [InlineData("null")]
    public void FromJson_EmptyOrBroken_ReturnsDefault(string? json)
    {
        Assert.Equal(FsRowFormat.Default, FsRowFormat.FromJson(json));
    }
}
