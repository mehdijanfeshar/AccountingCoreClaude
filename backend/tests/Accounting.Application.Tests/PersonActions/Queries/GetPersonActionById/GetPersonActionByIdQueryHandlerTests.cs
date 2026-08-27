using Accounting.Application.Common.Interfaces;
using Accounting.Application.PersonActions.Queries;
using Accounting.Application.PersonActions.Queries.GetPersonActionById;
using Moq;

namespace Accounting.Application.Tests.PersonActions.Queries.GetPersonActionById;

public sealed class GetPersonActionByIdQueryHandlerTests
{
    private static PersonActionDto SampleDto(Guid id) => new(
        Id: id,
        UserName: "John Doe",
        UserId: "jdoe",
        FromDate: "14030101",
        ToDate: "14031231",
        Status: true,
        OperatorRole: true,
        VahedCode: "0100",
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
        var readRepository = new Mock<IPersonActionReadRepository>();
        readRepository
            .Setup(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);

        var handler = new GetPersonActionByIdQueryHandler(readRepository.Object);

        var result = await handler.Handle(new GetPersonActionByIdQuery(id), CancellationToken.None);

        Assert.Same(expected, result);
    }

    [Fact]
    public async Task Handle_UnknownId_ReturnsNull_NotException()
    {
        var id = Guid.NewGuid();
        var readRepository = new Mock<IPersonActionReadRepository>();
        readRepository
            .Setup(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync((PersonActionDto?)null);

        var handler = new GetPersonActionByIdQueryHandler(readRepository.Object);

        var result = await handler.Handle(new GetPersonActionByIdQuery(id), CancellationToken.None);

        Assert.Null(result);
    }

    [Fact]
    public async Task Handle_PropagatesCancellationTokenToRepository()
    {
        var id = Guid.NewGuid();
        var readRepository = new Mock<IPersonActionReadRepository>();
        using var cts = new CancellationTokenSource();
        var token = cts.Token;
        readRepository
            .Setup(r => r.GetByIdAsync(id, token))
            .ReturnsAsync((PersonActionDto?)null);

        var handler = new GetPersonActionByIdQueryHandler(readRepository.Object);

        await handler.Handle(new GetPersonActionByIdQuery(id), token);

        readRepository.Verify(r => r.GetByIdAsync(id, token), Times.Once);
    }

    [Fact]
    public void Constructor_DoesNotDependOnIUnitOfWork()
    {
        var parameterTypes = typeof(GetPersonActionByIdQueryHandler)
            .GetConstructors()
            .SelectMany(c => c.GetParameters())
            .Select(p => p.ParameterType);

        Assert.DoesNotContain(typeof(IUnitOfWork), parameterTypes);
    }
}
