using Accounting.Application.PreDescribs.Queries;
using Accounting.Application.PreDescribs.Queries.GetPreDescribById;
using Accounting.Application.Common.Interfaces;
using Moq;

namespace Accounting.Application.Tests.PreDescribs.Queries.GetPreDescribById;

public sealed class GetPreDescribByIdQueryHandlerTests
{
    private static PreDescribDto SampleDto(Guid id) => new(
        Id: id,
        AccountId: null,
        Descrip: "توضیحات",
        AddUserId: "user1",
        VahedCode: "0001",
        FlagVoucher: false);

    [Fact]
    public async Task Handle_ExistingId_ReturnsRepositoryDto()
    {
        var id = Guid.NewGuid();
        var expected = SampleDto(id);
        var readRepository = new Mock<IPreDescribReadRepository>();
        readRepository
            .Setup(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);

        var handler = new GetPreDescribByIdQueryHandler(readRepository.Object);

        var result = await handler.Handle(new GetPreDescribByIdQuery(id), CancellationToken.None);

        Assert.Same(expected, result);
    }

    [Fact]
    public async Task Handle_UnknownId_ReturnsNull_NotException()
    {
        var id = Guid.NewGuid();
        var readRepository = new Mock<IPreDescribReadRepository>();
        readRepository
            .Setup(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync((PreDescribDto?)null);

        var handler = new GetPreDescribByIdQueryHandler(readRepository.Object);

        var result = await handler.Handle(new GetPreDescribByIdQuery(id), CancellationToken.None);

        Assert.Null(result);
    }

    [Fact]
    public async Task Handle_PropagatesCancellationTokenToRepository()
    {
        var id = Guid.NewGuid();
        var readRepository = new Mock<IPreDescribReadRepository>();
        using var cts = new CancellationTokenSource();
        var token = cts.Token;
        readRepository
            .Setup(r => r.GetByIdAsync(id, token))
            .ReturnsAsync((PreDescribDto?)null);

        var handler = new GetPreDescribByIdQueryHandler(readRepository.Object);

        await handler.Handle(new GetPreDescribByIdQuery(id), token);

        readRepository.Verify(r => r.GetByIdAsync(id, token), Times.Once);
    }

    [Fact]
    public void Constructor_DoesNotDependOnIUnitOfWork()
    {
        var parameterTypes = typeof(GetPreDescribByIdQueryHandler)
            .GetConstructors()
            .SelectMany(c => c.GetParameters())
            .Select(p => p.ParameterType);

        Assert.DoesNotContain(typeof(IUnitOfWork), parameterTypes);
    }
}
