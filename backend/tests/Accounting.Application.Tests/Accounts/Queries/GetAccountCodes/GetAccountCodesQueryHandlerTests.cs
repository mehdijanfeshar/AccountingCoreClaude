using Accounting.Application.Accounts.Queries;
using Accounting.Application.Accounts.Queries.GetAccountCodes;
using Accounting.Application.Common;
using Accounting.Application.Common.Interfaces;
using Moq;

namespace Accounting.Application.Tests.Accounts.Queries.GetAccountCodes;

public sealed class GetAccountCodesQueryHandlerTests
{
    private static AccountCodeDto SampleDto(Guid id) => new(
        Id: id,
        TypeCode: true,
        ParentId: null,
        AccCode: "100100",
        AccCodeName: "بانک ملی",
        TypeActivity: true,
        SourceAndConsumeId: null,
        IdentyGroupsId: null,
        TypeAccCode: true,
        CreatedDate: DateTime.UtcNow,
        UpdatedDate: null,
        AddUserId: "user1",
        ChangeUserId: null,
        IsDeleted: false,
        MoInforClose: null,
        TypeAction: null);

    [Fact]
    public async Task Handle_PassesPageNumberAndPageSizeToRepository_AndReturnsRepositoryResult()
    {
        var readRepository = new Mock<IAccountCodeReadRepository>();
        var expected = new PagedResult<AccountCodeDto>
        {
            Items = new[] { SampleDto(Guid.NewGuid()) },
            PageNumber = 2,
            PageSize = 25,
            TotalCount = 51,
        };
        readRepository
            .Setup(r => r.GetPagedAsync(2, 25, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);

        var handler = new GetAccountCodesQueryHandler(readRepository.Object);

        var result = await handler.Handle(new GetAccountCodesQuery(PageNumber: 2, PageSize: 25), CancellationToken.None);

        Assert.Same(expected, result);
        Assert.Equal(2, result.PageNumber);
        Assert.Equal(25, result.PageSize);
        Assert.Equal(51, result.TotalCount);
        readRepository.Verify(r => r.GetPagedAsync(2, 25, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_PropagatesCancellationTokenToRepository()
    {
        var readRepository = new Mock<IAccountCodeReadRepository>();
        using var cts = new CancellationTokenSource();
        var token = cts.Token;
        readRepository
            .Setup(r => r.GetPagedAsync(1, 20, token))
            .ReturnsAsync(new PagedResult<AccountCodeDto>());

        var handler = new GetAccountCodesQueryHandler(readRepository.Object);

        await handler.Handle(new GetAccountCodesQuery(PageNumber: 1, PageSize: 20), token);

        readRepository.Verify(r => r.GetPagedAsync(1, 20, token), Times.Once);
    }

    [Fact]
    public void Constructor_DoesNotDependOnIUnitOfWork()
    {
        // Read-side handlers must never touch IUnitOfWork — there is nothing to persist.
        var parameterTypes = typeof(GetAccountCodesQueryHandler)
            .GetConstructors()
            .SelectMany(c => c.GetParameters())
            .Select(p => p.ParameterType);

        Assert.DoesNotContain(typeof(IUnitOfWork), parameterTypes);
    }
}
