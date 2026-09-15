using Accounting.Application.AccountCodeInterfaces.Queries;
using Accounting.Application.AccountCodeInterfaces.Queries.GetAccountCodeInterfaces;
using Accounting.Application.Common;
using Accounting.Application.Common.Interfaces;
using Moq;

namespace Accounting.Application.Tests.AccountCodeInterfaces.Queries.GetAccountCodeInterfaces;

public sealed class GetAccountCodeInterfacesQueryHandlerTests
{
    private static AccountCodeInterfaceDto SampleDto(Guid id) => new(
        Id: id,
        Type: true,
        AccountCodeId: Guid.NewGuid(),
        CreatedDate: DateTime.UtcNow,
        UpdatedDate: null,
        AddUserId: "user1",
        ChangeUserId: null,
        IsDeleted: false);

    [Fact]
    public async Task Handle_PassesPageNumberAndPageSizeToRepository_AndReturnsRepositoryResult()
    {
        var readRepository = new Mock<IAccountCodeInterfaceReadRepository>();
        var expected = new PagedResult<AccountCodeInterfaceDto>
        {
            Items = new[] { SampleDto(Guid.NewGuid()) },
            PageNumber = 2,
            PageSize = 25,
            TotalCount = 51,
        };
        readRepository
            .Setup(r => r.GetPagedAsync(2, 25, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);

        var handler = new GetAccountCodeInterfacesQueryHandler(readRepository.Object);

        var result = await handler.Handle(new GetAccountCodeInterfacesQuery(PageNumber: 2, PageSize: 25), CancellationToken.None);

        Assert.Same(expected, result);
        Assert.Equal(2, result.PageNumber);
        Assert.Equal(25, result.PageSize);
        Assert.Equal(51, result.TotalCount);
        readRepository.Verify(r => r.GetPagedAsync(2, 25, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_PropagatesCancellationTokenToRepository()
    {
        var readRepository = new Mock<IAccountCodeInterfaceReadRepository>();
        using var cts = new CancellationTokenSource();
        var token = cts.Token;
        readRepository
            .Setup(r => r.GetPagedAsync(1, 20, token))
            .ReturnsAsync(new PagedResult<AccountCodeInterfaceDto>());

        var handler = new GetAccountCodeInterfacesQueryHandler(readRepository.Object);

        await handler.Handle(new GetAccountCodeInterfacesQuery(PageNumber: 1, PageSize: 20), token);

        readRepository.Verify(r => r.GetPagedAsync(1, 20, token), Times.Once);
    }

    [Fact]
    public void Constructor_DoesNotDependOnIUnitOfWork()
    {
        var parameterTypes = typeof(GetAccountCodeInterfacesQueryHandler)
            .GetConstructors()
            .SelectMany(c => c.GetParameters())
            .Select(p => p.ParameterType);

        Assert.DoesNotContain(typeof(IUnitOfWork), parameterTypes);
    }
}
