using Accounting.Application.CheckBooks.Queries;
using Accounting.Application.CheckBooks.Queries.GetCheckBooks;
using Accounting.Application.Common;
using Accounting.Application.Common.Interfaces;
using Moq;

namespace Accounting.Application.Tests.CheckBooks.Queries.GetCheckBooks;

public sealed class GetCheckBooksQueryHandlerTests
{
    private static CheckBookDto SampleDto(Guid id) => new(
        Id: id,
        AccountId: Guid.NewGuid(),
        CheckBookTitle: "دسته چک اول",
        CheckBookDate: "13990101",
        FromCheckNumber: "100000",
        ToCheckNumber: "100050",
        CheckTypeId: Guid.NewGuid(),
        VahedCode: "0001",
        CheckBookType: true,
        Serial: "SER0001",
        CreatedDate: DateTime.UtcNow,
        UpdatedDate: null,
        AddUserId: "user1",
        ChangeUserId: null,
        IsDeleted: false);

    [Fact]
    public async Task Handle_PassesPageNumberAndPageSizeToRepository_AndReturnsRepositoryResult()
    {
        var readRepository = new Mock<ICheckBookReadRepository>();
        var expected = new PagedResult<CheckBookDto>
        {
            Items = new[] { SampleDto(Guid.NewGuid()) },
            PageNumber = 2,
            PageSize = 25,
            TotalCount = 51,
        };
        readRepository
            .Setup(r => r.GetPagedAsync(2, 25, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);

        var handler = new GetCheckBooksQueryHandler(readRepository.Object);

        var result = await handler.Handle(new GetCheckBooksQuery(PageNumber: 2, PageSize: 25), CancellationToken.None);

        Assert.Same(expected, result);
        Assert.Equal(2, result.PageNumber);
        Assert.Equal(25, result.PageSize);
        Assert.Equal(51, result.TotalCount);
        readRepository.Verify(r => r.GetPagedAsync(2, 25, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_PropagatesCancellationTokenToRepository()
    {
        var readRepository = new Mock<ICheckBookReadRepository>();
        using var cts = new CancellationTokenSource();
        var token = cts.Token;
        readRepository
            .Setup(r => r.GetPagedAsync(1, 20, token))
            .ReturnsAsync(new PagedResult<CheckBookDto>());

        var handler = new GetCheckBooksQueryHandler(readRepository.Object);

        await handler.Handle(new GetCheckBooksQuery(PageNumber: 1, PageSize: 20), token);

        readRepository.Verify(r => r.GetPagedAsync(1, 20, token), Times.Once);
    }

    [Fact]
    public void Constructor_DoesNotDependOnIUnitOfWork()
    {
        var parameterTypes = typeof(GetCheckBooksQueryHandler)
            .GetConstructors()
            .SelectMany(c => c.GetParameters())
            .Select(p => p.ParameterType);

        Assert.DoesNotContain(typeof(IUnitOfWork), parameterTypes);
    }
}
