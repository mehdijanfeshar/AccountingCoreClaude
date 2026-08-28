using Accounting.Application.VahedInfos.Queries;
using Accounting.Application.VahedInfos.Queries.GetVahedInfoById;
using Accounting.Application.Common.Interfaces;
using Moq;

namespace Accounting.Application.Tests.VahedInfos.Queries.GetVahedInfoById;

public sealed class GetVahedInfoByIdQueryHandlerTests
{
    private static VahedInfoDto SampleDto(Guid id) => new(
        Id: id,
        VahedCode: "0001",
        VahedName: "واحد مرکزی",
        CityId: Guid.NewGuid(),
        VahedTypeId: Guid.NewGuid(),
        ParentId: null);

    [Fact]
    public async Task Handle_ExistingId_ReturnsRepositoryDto()
    {
        var id = Guid.NewGuid();
        var expected = SampleDto(id);
        var readRepository = new Mock<IVahedInfoReadRepository>();
        readRepository
            .Setup(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);

        var handler = new GetVahedInfoByIdQueryHandler(readRepository.Object);

        var result = await handler.Handle(new GetVahedInfoByIdQuery(id), CancellationToken.None);

        Assert.Same(expected, result);
    }

    [Fact]
    public async Task Handle_UnknownId_ReturnsNull_NotException()
    {
        var id = Guid.NewGuid();
        var readRepository = new Mock<IVahedInfoReadRepository>();
        readRepository
            .Setup(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync((VahedInfoDto?)null);

        var handler = new GetVahedInfoByIdQueryHandler(readRepository.Object);

        var result = await handler.Handle(new GetVahedInfoByIdQuery(id), CancellationToken.None);

        Assert.Null(result);
    }

    [Fact]
    public async Task Handle_PropagatesCancellationTokenToRepository()
    {
        var id = Guid.NewGuid();
        var readRepository = new Mock<IVahedInfoReadRepository>();
        using var cts = new CancellationTokenSource();
        var token = cts.Token;
        readRepository
            .Setup(r => r.GetByIdAsync(id, token))
            .ReturnsAsync((VahedInfoDto?)null);

        var handler = new GetVahedInfoByIdQueryHandler(readRepository.Object);

        await handler.Handle(new GetVahedInfoByIdQuery(id), token);

        readRepository.Verify(r => r.GetByIdAsync(id, token), Times.Once);
    }

    [Fact]
    public void Constructor_DoesNotDependOnIUnitOfWork()
    {
        var parameterTypes = typeof(GetVahedInfoByIdQueryHandler)
            .GetConstructors()
            .SelectMany(c => c.GetParameters())
            .Select(p => p.ParameterType);

        Assert.DoesNotContain(typeof(IUnitOfWork), parameterTypes);
    }
}
