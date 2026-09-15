using Accounting.Application.Accounts.Queries;
using Accounting.Application.Accounts.Queries.GetAccountTafsilGroupLinks;
using Accounting.Application.Common.Interfaces;
using Moq;

namespace Accounting.Application.Tests.Accounts.Queries.GetAccountTafsilGroupLinks;

public sealed class GetAccountTafsilGroupLinksQueryHandlerTests
{
    private static AccountTafsilGroupLinkDto SampleDto(Guid accountId) => new(
        Id: Guid.NewGuid(),
        AccountId: accountId,
        LevelId: Guid.NewGuid(),
        TafsilGroupId: Guid.NewGuid(),
        CreatedDate: DateTime.UtcNow,
        UpdatedDate: null,
        AddUserId: "user1",
        ChangeUserId: null,
        IsDeleted: false);

    [Fact]
    public async Task Handle_ReturnsRepositoryResult()
    {
        var accountCodeId = Guid.NewGuid();
        var expected = new[] { SampleDto(accountCodeId) };
        var readRepository = new Mock<IAccountCodeReadRepository>();
        readRepository
            .Setup(r => r.GetTafsilGroupLinksAsync(accountCodeId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);

        var handler = new GetAccountTafsilGroupLinksQueryHandler(readRepository.Object);

        var result = await handler.Handle(new GetAccountTafsilGroupLinksQuery(accountCodeId), CancellationToken.None);

        Assert.Same(expected, result);
    }

    [Fact]
    public async Task Handle_PropagatesCancellationTokenToRepository()
    {
        var accountCodeId = Guid.NewGuid();
        var readRepository = new Mock<IAccountCodeReadRepository>();
        using var cts = new CancellationTokenSource();
        var token = cts.Token;
        readRepository
            .Setup(r => r.GetTafsilGroupLinksAsync(accountCodeId, token))
            .ReturnsAsync(Array.Empty<AccountTafsilGroupLinkDto>());

        var handler = new GetAccountTafsilGroupLinksQueryHandler(readRepository.Object);

        await handler.Handle(new GetAccountTafsilGroupLinksQuery(accountCodeId), token);

        readRepository.Verify(r => r.GetTafsilGroupLinksAsync(accountCodeId, token), Times.Once);
    }

    [Fact]
    public void Constructor_DoesNotDependOnIUnitOfWork()
    {
        var parameterTypes = typeof(GetAccountTafsilGroupLinksQueryHandler)
            .GetConstructors()
            .SelectMany(c => c.GetParameters())
            .Select(p => p.ParameterType);

        Assert.DoesNotContain(typeof(IUnitOfWork), parameterTypes);
    }
}
