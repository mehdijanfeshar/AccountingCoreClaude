using Accounting.Application.CheckBooks.Queries;
using Accounting.Application.CheckBooks.Queries.GetCheckBookById;
using Accounting.Application.Common.Interfaces;
using Moq;

namespace Accounting.Application.Tests.CheckBooks.Queries.GetCheckBookById;

public sealed class GetCheckBookByIdQueryHandlerTests
{
    private static CheckBookDto SampleDto(Guid id) => new(
        Id: id,
        AccountId: Guid.NewGuid(),
        CheckBookTitle: "دسته چک اول",
        CheckBookDate: "13990101",
        FromCheckNumber: "100000",
        ToCheckNumber: "100050",
        CheckTypeId: Guid.NewGuid(),
        VahedCode: "0001",
        CheckBookType: true,
        Serial: "SER0001",
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
        var readRepository = new Mock<ICheckBookReadRepository>();
        readRepository
            .Setup(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);

        var handler = new GetCheckBookByIdQueryHandler(readRepository.Object);

        var result = await handler.Handle(new GetCheckBookByIdQuery(id), CancellationToken.None);

        Assert.Same(expected, result);
    }

    [Fact]
    public async Task Handle_UnknownId_ReturnsNull_NotException()
    {
        var id = Guid.NewGuid();
        var readRepository = new Mock<ICheckBookReadRepository>();
        readRepository
            .Setup(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync((CheckBookDto?)null);

        var handler = new GetCheckBookByIdQueryHandler(readRepository.Object);

        var result = await handler.Handle(new GetCheckBookByIdQuery(id), CancellationToken.None);

        Assert.Null(result);
    }

    [Fact]
    public async Task Handle_PropagatesCancellationTokenToRepository()
    {
        var id = Guid.NewGuid();
        var readRepository = new Mock<ICheckBookReadRepository>();
        using var cts = new CancellationTokenSource();
        var token = cts.Token;
        readRepository
            .Setup(r => r.GetByIdAsync(id, token))
            .ReturnsAsync((CheckBookDto?)null);

        var handler = new GetCheckBookByIdQueryHandler(readRepository.Object);

        await handler.Handle(new GetCheckBookByIdQuery(id), token);

        readRepository.Verify(r => r.GetByIdAsync(id, token), Times.Once);
    }

    [Fact]
    public void Constructor_DoesNotDependOnIUnitOfWork()
    {
        var parameterTypes = typeof(GetCheckBookByIdQueryHandler)
            .GetConstructors()
            .SelectMany(c => c.GetParameters())
            .Select(p => p.ParameterType);

        Assert.DoesNotContain(typeof(IUnitOfWork), parameterTypes);
    }
}
