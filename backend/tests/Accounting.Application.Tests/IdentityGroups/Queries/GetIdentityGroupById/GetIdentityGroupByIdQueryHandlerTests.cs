using Accounting.Application.IdentityGroups.Queries;
using Accounting.Application.IdentityGroups.Queries.GetIdentityGroupById;
using Accounting.Application.Common.Interfaces;
using Moq;

namespace Accounting.Application.Tests.IdentityGroups.Queries.GetIdentityGroupById;

public sealed class GetIdentityGroupByIdQueryHandlerTests
{
    private static IdentityGroupDto SampleDto(Guid id) => new(
        Id: id,
        IdentityGroupsDesc: "desc",
        IdentityGroupsCode: "001",
        VahedCode: "0100",
        TafsiliId: Guid.NewGuid(),
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
        var readRepository = new Mock<IIdentityGroupReadRepository>();
        readRepository
            .Setup(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);

        var handler = new GetIdentityGroupByIdQueryHandler(readRepository.Object);

        var result = await handler.Handle(new GetIdentityGroupByIdQuery(id), CancellationToken.None);

        Assert.Same(expected, result);
    }

    [Fact]
    public async Task Handle_UnknownId_ReturnsNull_NotException()
    {
        var id = Guid.NewGuid();
        var readRepository = new Mock<IIdentityGroupReadRepository>();
        readRepository
            .Setup(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync((IdentityGroupDto?)null);

        var handler = new GetIdentityGroupByIdQueryHandler(readRepository.Object);

        var result = await handler.Handle(new GetIdentityGroupByIdQuery(id), CancellationToken.None);

        Assert.Null(result);
    }

    [Fact]
    public async Task Handle_PropagatesCancellationTokenToRepository()
    {
        var id = Guid.NewGuid();
        var readRepository = new Mock<IIdentityGroupReadRepository>();
        using var cts = new CancellationTokenSource();
        var token = cts.Token;
        readRepository
            .Setup(r => r.GetByIdAsync(id, token))
            .ReturnsAsync((IdentityGroupDto?)null);

        var handler = new GetIdentityGroupByIdQueryHandler(readRepository.Object);

        await handler.Handle(new GetIdentityGroupByIdQuery(id), token);

        readRepository.Verify(r => r.GetByIdAsync(id, token), Times.Once);
    }

    [Fact]
    public void Constructor_DoesNotDependOnIUnitOfWork()
    {
        var parameterTypes = typeof(GetIdentityGroupByIdQueryHandler)
            .GetConstructors()
            .SelectMany(c => c.GetParameters())
            .Select(p => p.ParameterType);

        Assert.DoesNotContain(typeof(IUnitOfWork), parameterTypes);
    }
}
