using Accounting.Application.IdentityGroups.Queries;
using Accounting.Application.IdentityGroups.Queries.GetIdentityGroups;
using Accounting.Application.Common;
using Accounting.Application.Common.Interfaces;
using Moq;

namespace Accounting.Application.Tests.IdentityGroups.Queries.GetIdentityGroups;

public sealed class GetIdentityGroupsQueryHandlerTests
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
    public async Task Handle_PassesPageNumberAndPageSizeToRepository_AndReturnsRepositoryResult()
    {
        var readRepository = new Mock<IIdentityGroupReadRepository>();
        var expected = new PagedResult<IdentityGroupDto>
        {
            Items = new[] { SampleDto(Guid.NewGuid()) },
            PageNumber = 2,
            PageSize = 25,
            TotalCount = 51,
        };
        readRepository
            .Setup(r => r.GetPagedAsync(2, 25, It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);

        var handler = new GetIdentityGroupsQueryHandler(readRepository.Object);
        var query = new GetIdentityGroupsQuery(PageNumber: 2, PageSize: 25) { VahedCode = "0100" };

        var result = await handler.Handle(query, CancellationToken.None);

        Assert.Same(expected, result);
        Assert.Equal(2, result.PageNumber);
        Assert.Equal(25, result.PageSize);
        Assert.Equal(51, result.TotalCount);
        readRepository.Verify(r => r.GetPagedAsync(2, 25, It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_PassesRequestVahedCodeToRepository_AtFaceValue()
    {
        // Proves the handler trusts request.VahedCode as-is: by the time this handler runs,
        // VahedScopeBehavior has already overwritten it with the authenticated caller's own
        // unit code, so the handler must forward exactly that value, not derive its own.
        var readRepository = new Mock<IIdentityGroupReadRepository>();
        readRepository
            .Setup(r => r.GetPagedAsync(It.IsAny<int>(), It.IsAny<int>(), "0007", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PagedResult<IdentityGroupDto>());

        var handler = new GetIdentityGroupsQueryHandler(readRepository.Object);
        var query = new GetIdentityGroupsQuery(PageNumber: 1, PageSize: 20) { VahedCode = "0007" };

        await handler.Handle(query, CancellationToken.None);

        readRepository.Verify(
            r => r.GetPagedAsync(1, 20, "0007", It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_PropagatesCancellationTokenToRepository()
    {
        var readRepository = new Mock<IIdentityGroupReadRepository>();
        using var cts = new CancellationTokenSource();
        var token = cts.Token;
        readRepository
            .Setup(r => r.GetPagedAsync(1, 20, "0100", token))
            .ReturnsAsync(new PagedResult<IdentityGroupDto>());

        var handler = new GetIdentityGroupsQueryHandler(readRepository.Object);
        var query = new GetIdentityGroupsQuery(PageNumber: 1, PageSize: 20) { VahedCode = "0100" };

        await handler.Handle(query, token);

        readRepository.Verify(r => r.GetPagedAsync(1, 20, "0100", token), Times.Once);
    }

    [Fact]
    public void Constructor_DoesNotDependOnIUnitOfWork()
    {
        var parameterTypes = typeof(GetIdentityGroupsQueryHandler)
            .GetConstructors()
            .SelectMany(c => c.GetParameters())
            .Select(p => p.ParameterType);

        Assert.DoesNotContain(typeof(IUnitOfWork), parameterTypes);
    }
}
