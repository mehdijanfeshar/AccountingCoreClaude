using Accounting.Application.BillLogs.Queries;
using Accounting.Application.BillLogs.Queries.GetBillLogs;
using Accounting.Application.Common;
using Accounting.Application.Common.Interfaces;
using Moq;

namespace Accounting.Application.Tests.BillLogs.Queries.GetBillLogs;

public sealed class GetBillLogsQueryHandlerTests
{
    private static BillLogDto SampleDto(Guid id) => new(
        Id: id,
        LogDesc: "desc",
        LogDate: "14030101",
        VahedCode: "0100",
        Year: "1403",
        CreatedDate: DateTime.UtcNow,
        UpdatedDate: null,
        AddUserId: "user1",
        ChangeUserId: null,
        IsDeleted: false);

    [Fact]
    public async Task Handle_PassesPageNumberAndPageSizeToRepository_AndReturnsRepositoryResult()
    {
        var readRepository = new Mock<IBillLogReadRepository>();
        var expected = new PagedResult<BillLogDto>
        {
            Items = new[] { SampleDto(Guid.NewGuid()) },
            PageNumber = 2,
            PageSize = 25,
            TotalCount = 51,
        };
        readRepository
            .Setup(r => r.GetPagedAsync(2, 25, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);

        var handler = new GetBillLogsQueryHandler(readRepository.Object);

        var result = await handler.Handle(new GetBillLogsQuery(PageNumber: 2, PageSize: 25), CancellationToken.None);

        Assert.Same(expected, result);
        Assert.Equal(2, result.PageNumber);
        Assert.Equal(25, result.PageSize);
        Assert.Equal(51, result.TotalCount);
        readRepository.Verify(r => r.GetPagedAsync(2, 25, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_PropagatesCancellationTokenToRepository()
    {
        var readRepository = new Mock<IBillLogReadRepository>();
        using var cts = new CancellationTokenSource();
        var token = cts.Token;
        readRepository
            .Setup(r => r.GetPagedAsync(1, 20, token))
            .ReturnsAsync(new PagedResult<BillLogDto>());

        var handler = new GetBillLogsQueryHandler(readRepository.Object);

        await handler.Handle(new GetBillLogsQuery(PageNumber: 1, PageSize: 20), token);

        readRepository.Verify(r => r.GetPagedAsync(1, 20, token), Times.Once);
    }

    [Fact]
    public void Constructor_DoesNotDependOnIUnitOfWork()
    {
        var parameterTypes = typeof(GetBillLogsQueryHandler)
            .GetConstructors()
            .SelectMany(c => c.GetParameters())
            .Select(p => p.ParameterType);

        Assert.DoesNotContain(typeof(IUnitOfWork), parameterTypes);
    }
}
