using Accounting.Application.WhiteLists.Queries;
using Accounting.Application.WhiteLists.Queries.GetWhiteListById;
using Accounting.Application.Common.Interfaces;
using Moq;

namespace Accounting.Application.Tests.WhiteLists.Queries.GetWhiteListById;

public sealed class GetWhiteListByIdQueryHandlerTests
{
    private static WhiteListDto SampleDto(Guid id) => new(
        Id: id,
        AccountCodeId: Guid.NewGuid(),
        VahedTypeId: Guid.NewGuid(),
        VahedInfoId: Guid.NewGuid(),
        CreatedDate: DateTime.UtcNow,
        UpdatedDate: null,
        AddUserId: "user1",
        ChangeUserId: null,
        IsDeleted: false,
        FromAuthorizedDate: "14030101",
        ToAuthorizedDate: "14031231",
        FromLimitationDate: "14030101",
        ToLimitationDate: "14031231");

    [Fact]
    public async Task Handle_ExistingId_ReturnsRepositoryDto()
    {
        var id = Guid.NewGuid();
        var expected = SampleDto(id);
        var readRepository = new Mock<IWhiteListReadRepository>();
        readRepository
            .Setup(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);

        var handler = new GetWhiteListByIdQueryHandler(readRepository.Object);

        var result = await handler.Handle(new GetWhiteListByIdQuery(id), CancellationToken.None);

        Assert.Same(expected, result);
    }

    [Fact]
    public async Task Handle_UnknownId_ReturnsNull_NotException()
    {
        var id = Guid.NewGuid();
        var readRepository = new Mock<IWhiteListReadRepository>();
        readRepository
            .Setup(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync((WhiteListDto?)null);

        var handler = new GetWhiteListByIdQueryHandler(readRepository.Object);

        var result = await handler.Handle(new GetWhiteListByIdQuery(id), CancellationToken.None);

        Assert.Null(result);
    }

    [Fact]
    public async Task Handle_PropagatesCancellationTokenToRepository()
    {
        var id = Guid.NewGuid();
        var readRepository = new Mock<IWhiteListReadRepository>();
        using var cts = new CancellationTokenSource();
        var token = cts.Token;
        readRepository
            .Setup(r => r.GetByIdAsync(id, token))
            .ReturnsAsync((WhiteListDto?)null);

        var handler = new GetWhiteListByIdQueryHandler(readRepository.Object);

        await handler.Handle(new GetWhiteListByIdQuery(id), token);

        readRepository.Verify(r => r.GetByIdAsync(id, token), Times.Once);
    }

    [Fact]
    public void Constructor_DoesNotDependOnIUnitOfWork()
    {
        var parameterTypes = typeof(GetWhiteListByIdQueryHandler)
            .GetConstructors()
            .SelectMany(c => c.GetParameters())
            .Select(p => p.ParameterType);

        Assert.DoesNotContain(typeof(IUnitOfWork), parameterTypes);
    }
}
