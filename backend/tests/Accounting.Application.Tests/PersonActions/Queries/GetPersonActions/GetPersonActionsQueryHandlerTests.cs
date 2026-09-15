using Accounting.Application.Common;
using Accounting.Application.Common.Interfaces;
using Accounting.Application.PersonActions.Queries;
using Accounting.Application.PersonActions.Queries.GetPersonActions;
using Moq;

namespace Accounting.Application.Tests.PersonActions.Queries.GetPersonActions;

public sealed class GetPersonActionsQueryHandlerTests
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
    public async Task Handle_PassesPageNumberAndPageSizeToRepository_AndReturnsRepositoryResult()
    {
        var readRepository = new Mock<IPersonActionReadRepository>();
        var expected = new PagedResult<PersonActionDto>
        {
            Items = new[] { SampleDto(Guid.NewGuid()) },
            PageNumber = 2,
            PageSize = 25,
            TotalCount = 51,
        };
        readRepository
            .Setup(r => r.GetPagedAsync(2, 25, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);

        var handler = new GetPersonActionsQueryHandler(readRepository.Object);

        var result = await handler.Handle(new GetPersonActionsQuery(PageNumber: 2, PageSize: 25), CancellationToken.None);

        Assert.Same(expected, result);
        Assert.Equal(2, result.PageNumber);
        Assert.Equal(25, result.PageSize);
        Assert.Equal(51, result.TotalCount);
        readRepository.Verify(r => r.GetPagedAsync(2, 25, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_PropagatesCancellationTokenToRepository()
    {
        var readRepository = new Mock<IPersonActionReadRepository>();
        using var cts = new CancellationTokenSource();
        var token = cts.Token;
        readRepository
            .Setup(r => r.GetPagedAsync(1, 20, token))
            .ReturnsAsync(new PagedResult<PersonActionDto>());

        var handler = new GetPersonActionsQueryHandler(readRepository.Object);

        await handler.Handle(new GetPersonActionsQuery(PageNumber: 1, PageSize: 20), token);

        readRepository.Verify(r => r.GetPagedAsync(1, 20, token), Times.Once);
    }

    [Fact]
    public void Constructor_DoesNotDependOnIUnitOfWork()
    {
        var parameterTypes = typeof(GetPersonActionsQueryHandler)
            .GetConstructors()
            .SelectMany(c => c.GetParameters())
            .Select(p => p.ParameterType);

        Assert.DoesNotContain(typeof(IUnitOfWork), parameterTypes);
    }
}
