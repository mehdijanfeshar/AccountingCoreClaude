using Accounting.Application.IdentitySubGroups.Queries;
using Accounting.Application.IdentitySubGroups.Queries.GetIdentitySubGroupById;
using Accounting.Application.Common.Interfaces;
using Moq;

namespace Accounting.Application.Tests.IdentitySubGroups.Queries.GetIdentitySubGroupById;

public sealed class GetIdentitySubGroupByIdQueryHandlerTests
{
    private static IdentitySubGroupDto SampleDto(Guid id) => new(
        Id: id,
        IdentyGroupsId: Guid.NewGuid(),
        SubgrpsDesc: "desc",
        SubgrpsLen: 4,
        SumFlag: true,
        Fixed: false,
        SubgrpsType: true,
        VahedCode: "0100",
        Year: "1403",
        IdentySubGroupsCode: "01",
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
        var readRepository = new Mock<IIdentitySubGroupReadRepository>();
        readRepository
            .Setup(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);

        var handler = new GetIdentitySubGroupByIdQueryHandler(readRepository.Object);

        var result = await handler.Handle(new GetIdentitySubGroupByIdQuery(id), CancellationToken.None);

        Assert.Same(expected, result);
    }

    [Fact]
    public async Task Handle_UnknownId_ReturnsNull_NotException()
    {
        var id = Guid.NewGuid();
        var readRepository = new Mock<IIdentitySubGroupReadRepository>();
        readRepository
            .Setup(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync((IdentitySubGroupDto?)null);

        var handler = new GetIdentitySubGroupByIdQueryHandler(readRepository.Object);

        var result = await handler.Handle(new GetIdentitySubGroupByIdQuery(id), CancellationToken.None);

        Assert.Null(result);
    }

    [Fact]
    public async Task Handle_PropagatesCancellationTokenToRepository()
    {
        var id = Guid.NewGuid();
        var readRepository = new Mock<IIdentitySubGroupReadRepository>();
        using var cts = new CancellationTokenSource();
        var token = cts.Token;
        readRepository
            .Setup(r => r.GetByIdAsync(id, token))
            .ReturnsAsync((IdentitySubGroupDto?)null);

        var handler = new GetIdentitySubGroupByIdQueryHandler(readRepository.Object);

        await handler.Handle(new GetIdentitySubGroupByIdQuery(id), token);

        readRepository.Verify(r => r.GetByIdAsync(id, token), Times.Once);
    }

    [Fact]
    public void Constructor_DoesNotDependOnIUnitOfWork()
    {
        var parameterTypes = typeof(GetIdentitySubGroupByIdQueryHandler)
            .GetConstructors()
            .SelectMany(c => c.GetParameters())
            .Select(p => p.ParameterType);

        Assert.DoesNotContain(typeof(IUnitOfWork), parameterTypes);
    }
}
