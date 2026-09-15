using Accounting.Application.LevelTafsils.Queries;
using Accounting.Application.LevelTafsils.Queries.GetLevelTafsilById;
using Accounting.Application.Common.Interfaces;
using Moq;

namespace Accounting.Application.Tests.LevelTafsils.Queries.GetLevelTafsilById;

public sealed class GetLevelTafsilByIdQueryHandlerTests
{
    private static LevelTafsilDto SampleDto(Guid id) => new(
        Id: id,
        LevelCode: "01",
        LevelName: "سطح یک",
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
        var readRepository = new Mock<ILevelTafsilReadRepository>();
        readRepository
            .Setup(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);

        var handler = new GetLevelTafsilByIdQueryHandler(readRepository.Object);

        var result = await handler.Handle(new GetLevelTafsilByIdQuery(id), CancellationToken.None);

        Assert.Same(expected, result);
    }

    [Fact]
    public async Task Handle_UnknownId_ReturnsNull_NotException()
    {
        var id = Guid.NewGuid();
        var readRepository = new Mock<ILevelTafsilReadRepository>();
        readRepository
            .Setup(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync((LevelTafsilDto?)null);

        var handler = new GetLevelTafsilByIdQueryHandler(readRepository.Object);

        var result = await handler.Handle(new GetLevelTafsilByIdQuery(id), CancellationToken.None);

        Assert.Null(result);
    }

    [Fact]
    public async Task Handle_PropagatesCancellationTokenToRepository()
    {
        var id = Guid.NewGuid();
        var readRepository = new Mock<ILevelTafsilReadRepository>();
        using var cts = new CancellationTokenSource();
        var token = cts.Token;
        readRepository
            .Setup(r => r.GetByIdAsync(id, token))
            .ReturnsAsync((LevelTafsilDto?)null);

        var handler = new GetLevelTafsilByIdQueryHandler(readRepository.Object);

        await handler.Handle(new GetLevelTafsilByIdQuery(id), token);

        readRepository.Verify(r => r.GetByIdAsync(id, token), Times.Once);
    }

    [Fact]
    public void Constructor_DoesNotDependOnIUnitOfWork()
    {
        var parameterTypes = typeof(GetLevelTafsilByIdQueryHandler)
            .GetConstructors()
            .SelectMany(c => c.GetParameters())
            .Select(p => p.ParameterType);

        Assert.DoesNotContain(typeof(IUnitOfWork), parameterTypes);
    }
}
