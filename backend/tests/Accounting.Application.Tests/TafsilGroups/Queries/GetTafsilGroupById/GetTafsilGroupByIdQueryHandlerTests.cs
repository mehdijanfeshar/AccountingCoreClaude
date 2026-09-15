using Accounting.Application.TafsilGroups.Queries;
using Accounting.Application.TafsilGroups.Queries.GetTafsilGroupById;
using Accounting.Application.Common.Interfaces;
using Moq;

namespace Accounting.Application.Tests.TafsilGroups.Queries.GetTafsilGroupById;

public sealed class GetTafsilGroupByIdQueryHandlerTests
{
    private static TafsilGroupDto SampleDto(Guid id) => new(
        Id: id,
        TafsilGroupCode: "001",
        TafsilGroupName: "گروه تفصیلی یک",
        PersonType: null,
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
        var readRepository = new Mock<ITafsilGroupReadRepository>();
        readRepository
            .Setup(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);

        var handler = new GetTafsilGroupByIdQueryHandler(readRepository.Object);

        var result = await handler.Handle(new GetTafsilGroupByIdQuery(id), CancellationToken.None);

        Assert.Same(expected, result);
    }

    [Fact]
    public async Task Handle_UnknownId_ReturnsNull_NotException()
    {
        var id = Guid.NewGuid();
        var readRepository = new Mock<ITafsilGroupReadRepository>();
        readRepository
            .Setup(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync((TafsilGroupDto?)null);

        var handler = new GetTafsilGroupByIdQueryHandler(readRepository.Object);

        var result = await handler.Handle(new GetTafsilGroupByIdQuery(id), CancellationToken.None);

        Assert.Null(result);
    }

    [Fact]
    public async Task Handle_PropagatesCancellationTokenToRepository()
    {
        var id = Guid.NewGuid();
        var readRepository = new Mock<ITafsilGroupReadRepository>();
        using var cts = new CancellationTokenSource();
        var token = cts.Token;
        readRepository
            .Setup(r => r.GetByIdAsync(id, token))
            .ReturnsAsync((TafsilGroupDto?)null);

        var handler = new GetTafsilGroupByIdQueryHandler(readRepository.Object);

        await handler.Handle(new GetTafsilGroupByIdQuery(id), token);

        readRepository.Verify(r => r.GetByIdAsync(id, token), Times.Once);
    }

    [Fact]
    public void Constructor_DoesNotDependOnIUnitOfWork()
    {
        var parameterTypes = typeof(GetTafsilGroupByIdQueryHandler)
            .GetConstructors()
            .SelectMany(c => c.GetParameters())
            .Select(p => p.ParameterType);

        Assert.DoesNotContain(typeof(IUnitOfWork), parameterTypes);
    }
}
