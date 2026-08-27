using Accounting.Application.PreDescribs.Queries;
using Accounting.Application.PreDescribs.Queries.GetPreDescribs;
using Accounting.Application.Common;
using Accounting.Application.Common.Interfaces;
using Moq;

namespace Accounting.Application.Tests.PreDescribs.Queries.GetPreDescribs;

public sealed class GetPreDescribsQueryHandlerTests
{
    private static PreDescribDto SampleDto(Guid id) => new(
        Id: id,
        AccountId: null,
        Descrip: "توضیحات",
        AddUserId: "user1",
        VahedCode: "0001",
        FlagVoucher: false);

    [Fact]
    public async Task Handle_PassesPageNumberAndPageSizeToRepository_AndReturnsRepositoryResult()
    {
        var readRepository = new Mock<IPreDescribReadRepository>();
        var expected = new PagedResult<PreDescribDto>
        {
            Items = new[] { SampleDto(Guid.NewGuid()) },
            PageNumber = 2,
            PageSize = 25,
            TotalCount = 51,
        };
        readRepository
            .Setup(r => r.GetPagedAsync(2, 25, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);

        var handler = new GetPreDescribsQueryHandler(readRepository.Object);

        var result = await handler.Handle(new GetPreDescribsQuery(PageNumber: 2, PageSize: 25), CancellationToken.None);

        Assert.Same(expected, result);
        Assert.Equal(2, result.PageNumber);
        Assert.Equal(25, result.PageSize);
        Assert.Equal(51, result.TotalCount);
        readRepository.Verify(r => r.GetPagedAsync(2, 25, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_PropagatesCancellationTokenToRepository()
    {
        var readRepository = new Mock<IPreDescribReadRepository>();
        using var cts = new CancellationTokenSource();
        var token = cts.Token;
        readRepository
            .Setup(r => r.GetPagedAsync(1, 20, token))
            .ReturnsAsync(new PagedResult<PreDescribDto>());

        var handler = new GetPreDescribsQueryHandler(readRepository.Object);

        await handler.Handle(new GetPreDescribsQuery(PageNumber: 1, PageSize: 20), token);

        readRepository.Verify(r => r.GetPagedAsync(1, 20, token), Times.Once);
    }

    [Fact]
    public void Constructor_DoesNotDependOnIUnitOfWork()
    {
        var parameterTypes = typeof(GetPreDescribsQueryHandler)
            .GetConstructors()
            .SelectMany(c => c.GetParameters())
            .Select(p => p.ParameterType);

        Assert.DoesNotContain(typeof(IUnitOfWork), parameterTypes);
    }
}
