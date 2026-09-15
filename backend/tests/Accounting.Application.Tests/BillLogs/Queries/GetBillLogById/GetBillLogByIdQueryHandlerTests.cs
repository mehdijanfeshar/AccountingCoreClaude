using Accounting.Application.BillLogs.Queries;
using Accounting.Application.BillLogs.Queries.GetBillLogById;
using Accounting.Application.Common.Interfaces;
using Moq;

namespace Accounting.Application.Tests.BillLogs.Queries.GetBillLogById;

public sealed class GetBillLogByIdQueryHandlerTests
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
    public async Task Handle_ExistingId_ReturnsRepositoryDto()
    {
        var id = Guid.NewGuid();
        var expected = SampleDto(id);
        var readRepository = new Mock<IBillLogReadRepository>();
        readRepository
            .Setup(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);

        var handler = new GetBillLogByIdQueryHandler(readRepository.Object);

        var result = await handler.Handle(new GetBillLogByIdQuery(id), CancellationToken.None);

        Assert.Same(expected, result);
    }

    [Fact]
    public async Task Handle_UnknownId_ReturnsNull_NotException()
    {
        var id = Guid.NewGuid();
        var readRepository = new Mock<IBillLogReadRepository>();
        readRepository
            .Setup(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync((BillLogDto?)null);

        var handler = new GetBillLogByIdQueryHandler(readRepository.Object);

        var result = await handler.Handle(new GetBillLogByIdQuery(id), CancellationToken.None);

        Assert.Null(result);
    }

    [Fact]
    public async Task Handle_PropagatesCancellationTokenToRepository()
    {
        var id = Guid.NewGuid();
        var readRepository = new Mock<IBillLogReadRepository>();
        using var cts = new CancellationTokenSource();
        var token = cts.Token;
        readRepository
            .Setup(r => r.GetByIdAsync(id, token))
            .ReturnsAsync((BillLogDto?)null);

        var handler = new GetBillLogByIdQueryHandler(readRepository.Object);

        await handler.Handle(new GetBillLogByIdQuery(id), token);

        readRepository.Verify(r => r.GetByIdAsync(id, token), Times.Once);
    }

    [Fact]
    public void Constructor_DoesNotDependOnIUnitOfWork()
    {
        var parameterTypes = typeof(GetBillLogByIdQueryHandler)
            .GetConstructors()
            .SelectMany(c => c.GetParameters())
            .Select(p => p.ParameterType);

        Assert.DoesNotContain(typeof(IUnitOfWork), parameterTypes);
    }
}
