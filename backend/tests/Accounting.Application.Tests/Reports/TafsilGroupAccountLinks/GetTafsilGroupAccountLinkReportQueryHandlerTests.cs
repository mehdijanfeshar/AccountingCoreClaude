using Accounting.Application.Common.Interfaces;
using Accounting.Application.Reports.TafsilGroupAccountLinks;
using Moq;

namespace Accounting.Application.Tests.Reports.TafsilGroupAccountLinks;

/// <summary>گزارش فراگیر «ارتباط گروه تفصیلی با معین» — فقط پوشاندن ورودی به فیلتر و برگرداندن نتیجهٔ Repository.</summary>
public class GetTafsilGroupAccountLinkReportQueryHandlerTests
{
    private readonly Mock<ITafsilGroupAccountLinkReportReadRepository> _repo = new();

    private GetTafsilGroupAccountLinkReportQueryHandler Handler() => new(_repo.Object);

    [Fact]
    public async Task Builds_filter_from_query_and_returns_repository_result()
    {
        var accountIds = new[] { Guid.NewGuid() };
        var levelId = Guid.NewGuid();
        var groupId = Guid.NewGuid();
        var tafsiliId = Guid.NewGuid();
        var expected = new[]
        {
            new TafsilGroupAccountLinkReportRowDto(accountIds[0], "005000", "بدهکاران", levelId, "سطح ۱", groupId, "G1", "گروه ۱"),
        };
        TafsilGroupAccountLinkFilter? captured = null;
        _repo.Setup(r => r.GetAsync(It.IsAny<TafsilGroupAccountLinkFilter>(), It.IsAny<CancellationToken>()))
            .Callback<TafsilGroupAccountLinkFilter, CancellationToken>((f, _) => captured = f)
            .ReturnsAsync(expected);

        var result = await Handler().Handle(
            new GetTafsilGroupAccountLinkReportQuery(accountIds, levelId, groupId, tafsiliId), default);

        Assert.Same(expected, result);
        Assert.NotNull(captured);
        Assert.Equal(accountIds, captured!.AccountIds);
        Assert.Equal(levelId, captured.LevelId);
        Assert.Equal(groupId, captured.TafsilGroupId);
        Assert.Equal(tafsiliId, captured.TafsiliId);
    }

    [Fact]
    public async Task All_filters_optional()
    {
        _repo.Setup(r => r.GetAsync(It.IsAny<TafsilGroupAccountLinkFilter>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<TafsilGroupAccountLinkReportRowDto>());

        var result = await Handler().Handle(new GetTafsilGroupAccountLinkReportQuery(null, null, null, null), default);

        Assert.Empty(result);
    }

    [Fact]
    public void Validator_rejects_more_than_200_account_ids()
    {
        var v = new GetTafsilGroupAccountLinkReportQueryValidator();
        var tooMany = Enumerable.Range(0, 201).Select(_ => Guid.NewGuid()).ToList();
        Assert.False(v.Validate(new GetTafsilGroupAccountLinkReportQuery(tooMany, null, null, null)).IsValid);
        Assert.True(v.Validate(new GetTafsilGroupAccountLinkReportQuery(null, null, null, null)).IsValid);
        Assert.True(v.Validate(new GetTafsilGroupAccountLinkReportQuery(new[] { Guid.NewGuid() }, null, null, null)).IsValid);
    }
}
