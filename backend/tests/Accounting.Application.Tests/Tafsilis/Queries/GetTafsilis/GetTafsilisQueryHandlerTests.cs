using Accounting.Application.Tafsilis.Queries;
using Accounting.Application.Tafsilis.Queries.GetTafsilis;
using Accounting.Application.Common;
using Accounting.Application.Common.Interfaces;
using Moq;

namespace Accounting.Application.Tests.Tafsilis.Queries.GetTafsilis;

public sealed class GetTafsilisQueryHandlerTests
{
    private static TafsiliDto SampleDto(Guid id) => new(
        Id: id,
        TafsiliCode: "001",
        TafsiliName: "تفصیلی یک",
        TafsilDesc: null,
        IsActive: true,
        PersonType: null,
        Owner: null,
        VahedType: null,
        VahedCode: "1001",
        TafsilGroupIds: Array.Empty<Guid>(),
        CreatedDate: DateTime.UtcNow,
        UpdatedDate: null,
        AddUserId: "user1",
        ChangeUserId: null,
        IsDeleted: false);

    [Fact]
    public async Task Handle_PassesPageNumberPageSizeAndVahedCodeToRepository_AndReturnsRepositoryResult()
    {
        var readRepository = new Mock<ITafsiliReadRepository>();
        var expected = new PagedResult<TafsiliDto>
        {
            Items = new[] { SampleDto(Guid.NewGuid()) },
            PageNumber = 2,
            PageSize = 25,
            TotalCount = 51,
        };
        readRepository
            .Setup(r => r.GetPagedAsync(2, 25, "1001", It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);

        var handler = new GetTafsilisQueryHandler(readRepository.Object);
        var query = new GetTafsilisQuery(PageNumber: 2, PageSize: 25) { VahedCode = "1001" };

        var result = await handler.Handle(query, CancellationToken.None);

        Assert.Same(expected, result);
        readRepository.Verify(r => r.GetPagedAsync(2, 25, "1001", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public void Constructor_DoesNotDependOnIUnitOfWork()
    {
        var parameterTypes = typeof(GetTafsilisQueryHandler)
            .GetConstructors()
            .SelectMany(c => c.GetParameters())
            .Select(p => p.ParameterType);

        Assert.DoesNotContain(typeof(IUnitOfWork), parameterTypes);
    }
}
