using Accounting.Application.WhiteAndBlackLists.Queries;
using Accounting.Application.WhiteAndBlackLists.Queries.GetWhiteAndBlackListById;
using Accounting.Application.Common.Interfaces;
using Moq;

namespace Accounting.Application.Tests.WhiteAndBlackLists.Queries.GetWhiteAndBlackListById;

public sealed class GetWhiteAndBlackListByIdQueryHandlerTests
{
    private static WhiteAndBlackListDto SampleDto(Guid id) => new(
        Id: id,
        AccountCodeId: Guid.NewGuid(),
        VahedTypeId: Guid.NewGuid(),
        CreatedDate: DateTime.UtcNow,
        UpdatedDate: null,
        AddUserId: "user1",
        ChangeUserId: null,
        IsDeleted: false,
        FromAuthorizedDate: "14030101",
        ToAuthorizedDate: "14031231",
        FromLimitationDate: "14030101",
        ToLimitationDate: "14031231",
        State: true);

    [Fact]
    public async Task Handle_ExistingId_ReturnsRepositoryDto()
    {
        var id = Guid.NewGuid();
        var expected = SampleDto(id);
        var readRepository = new Mock<IWhiteAndBlackListReadRepository>();
        readRepository
            .Setup(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);

        var handler = new GetWhiteAndBlackListByIdQueryHandler(readRepository.Object);

        var result = await handler.Handle(new GetWhiteAndBlackListByIdQuery(id), CancellationToken.None);

        Assert.Same(expected, result);
    }

    [Fact]
    public async Task Handle_UnknownId_ReturnsNull_NotException()
    {
        var id = Guid.NewGuid();
        var readRepository = new Mock<IWhiteAndBlackListReadRepository>();
        readRepository
            .Setup(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync((WhiteAndBlackListDto?)null);

        var handler = new GetWhiteAndBlackListByIdQueryHandler(readRepository.Object);

        var result = await handler.Handle(new GetWhiteAndBlackListByIdQuery(id), CancellationToken.None);

        Assert.Null(result);
    }

    [Fact]
    public async Task Handle_PropagatesCancellationTokenToRepository()
    {
        var id = Guid.NewGuid();
        var readRepository = new Mock<IWhiteAndBlackListReadRepository>();
        using var cts = new CancellationTokenSource();
        var token = cts.Token;
        readRepository
            .Setup(r => r.GetByIdAsync(id, token))
            .ReturnsAsync((WhiteAndBlackListDto?)null);

        var handler = new GetWhiteAndBlackListByIdQueryHandler(readRepository.Object);

        await handler.Handle(new GetWhiteAndBlackListByIdQuery(id), token);

        readRepository.Verify(r => r.GetByIdAsync(id, token), Times.Once);
    }

    [Fact]
    public void Constructor_DoesNotDependOnIUnitOfWork()
    {
        var parameterTypes = typeof(GetWhiteAndBlackListByIdQueryHandler)
            .GetConstructors()
            .SelectMany(c => c.GetParameters())
            .Select(p => p.ParameterType);

        Assert.DoesNotContain(typeof(IUnitOfWork), parameterTypes);
    }
}
