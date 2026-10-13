using Accounting.Application.AuditLogs;
using Accounting.Application.Common;
using Accounting.Application.Common.Security;
using Moq;

namespace Accounting.Application.Tests.AuditLogs;

public class GetAuditLogsTests
{
    private static GetAuditLogsQuery Q(
        DateTime? from = null, DateTime? to = null, string? user = null, string? table = null, string? action = null,
        string? search = null, int page = 1, int size = 50, ReportUnitScopeMode scope = ReportUnitScopeMode.Self)
        => new(from, to, user, table, action, null, null, search, page, size) { UnitScope = scope, VahedCode = "0002" };

    // ---------------- Validator
    [Fact]
    public void Validator_DefaultQuery_IsValid()
        => Assert.True(new GetAuditLogsQueryValidator().Validate(Q()).IsValid);

    [Fact]
    public void Validator_AllUnitsScope_Rejected()
        => Assert.False(new GetAuditLogsQueryValidator().Validate(Q(scope: ReportUnitScopeMode.AllUnits)).IsValid);

    [Fact]
    public void Validator_WithSubUnits_Allowed()
        => Assert.True(new GetAuditLogsQueryValidator().Validate(Q(scope: ReportUnitScopeMode.WithSubUnits)).IsValid);

    [Theory]
    [InlineData(0, 50, false)]
    [InlineData(1, 0, false)]
    [InlineData(1, 201, false)]
    [InlineData(1, 200, true)]
    public void Validator_Paging(int page, int size, bool ok)
        => Assert.Equal(ok, new GetAuditLogsQueryValidator().Validate(Q(page: page, size: size)).IsValid);

    [Fact]
    public void Validator_ToDateMustBeAfterFromDate()
    {
        var d = new DateTime(2026, 1, 1);
        var v = new GetAuditLogsQueryValidator();
        Assert.False(v.Validate(Q(from: d, to: d)).IsValid);
        Assert.False(v.Validate(Q(from: d, to: d.AddDays(-1))).IsValid);
        Assert.True(v.Validate(Q(from: d, to: d.AddDays(1))).IsValid);
        Assert.True(v.Validate(Q(to: d)).IsValid);
    }

    [Fact]
    public void Validator_LengthLimits()
    {
        var v = new GetAuditLogsQueryValidator();
        Assert.False(v.Validate(Q(user: new string('1', 11))).IsValid);
        Assert.False(v.Validate(Q(search: new string('x', 101))).IsValid);
        Assert.True(v.Validate(Q(user: new string('1', 10), search: new string('x', 100))).IsValid);
    }

    // ---------------- Handler
    [Fact]
    public async Task Handler_NoScopeSet_UsesRequestVahedCode_AndTrimsFilters()
    {
        var repo = new Mock<IAuditLogReadRepository>();
        AuditLogFilter? captured = null;
        repo.Setup(r => r.SearchAsync(It.IsAny<AuditLogFilter>(), 2, 10, It.IsAny<CancellationToken>()))
            .Callback<AuditLogFilter, int, int, CancellationToken>((f, _, _, _) => captured = f)
            .ReturnsAsync((PagedResult<AuditLogDto>)null!);
        var h = new GetAuditLogsQueryHandler(repo.Object, new ReportUnitScope());

        await h.Handle(Q(user: " 123 ", table: "  ", action: null, search: " abc ", page: 2, size: 10), default);

        Assert.Equal(new[] { "0002" }, captured!.VahedCodes);
        Assert.Equal("123", captured.UserId);
        Assert.Null(captured.TableName);
        Assert.Null(captured.ActionType);
        Assert.Equal("abc", captured.Search);
    }

    [Fact]
    public async Task Handler_ScopeSet_UsesScopeCodes()
    {
        var repo = new Mock<IAuditLogReadRepository>();
        AuditLogFilter? captured = null;
        repo.Setup(r => r.SearchAsync(It.IsAny<AuditLogFilter>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .Callback<AuditLogFilter, int, int, CancellationToken>((f, _, _, _) => captured = f)
            .ReturnsAsync((PagedResult<AuditLogDto>)null!);
        var scope = new ReportUnitScope();
        scope.Set(["0002", "0003"]);

        await new GetAuditLogsQueryHandler(repo.Object, scope).Handle(Q(), default);

        Assert.Equal(new[] { "0002", "0003" }, captured!.VahedCodes);
    }

    [Fact]
    public async Task TablesHandler_ReturnsCatalogOrderedByTitle()
    {
        var res = await new GetAuditTablesQueryHandler().Handle(new GetAuditTablesQuery(), default);
        Assert.NotEmpty(res);
        Assert.Equal(res.OrderBy(t => t.Title, StringComparer.Ordinal).Select(t => t.Table), res.Select(t => t.Table));
        Assert.Equal(res.Count, res.Select(t => t.Table).Distinct().Count());
    }
}
