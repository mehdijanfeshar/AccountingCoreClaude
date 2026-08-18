using Accounting.Application.Accounts.Queries;
using Accounting.Application.Accounts.Queries.GetAccountCodeById;
using Accounting.Application.Common.Interfaces;
using Moq;

namespace Accounting.Application.Tests.Accounts.Queries.GetAccountCodeById;

public sealed class GetAccountCodeByIdQueryHandlerTests
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
    public async Task Handle_ExistingId_ReturnsRepositoryDto()
    {
        var id = Guid.NewGuid();
        var expected = SampleDto(id);
        var readRepository = new Mock<IAccountCodeReadRepository>();
        readRepository
            .Setup(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);

        var handler = new GetAccountCodeByIdQueryHandler(readRepository.Object);

        var result = await handler.Handle(new GetAccountCodeByIdQuery(id), CancellationToken.None);

        Assert.Same(expected, result);
    }

    [Fact]
    public async Task Handle_UnknownId_ReturnsNull_NotException()
    {
        var id = Guid.NewGuid();
        var readRepository = new Mock<IAccountCodeReadRepository>();
        readRepository
            .Setup(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync((AccountCodeDto?)null);

        var handler = new GetAccountCodeByIdQueryHandler(readRepository.Object);

        var result = await handler.Handle(new GetAccountCodeByIdQuery(id), CancellationToken.None);

        Assert.Null(result);
    }

    [Fact]
    public async Task Handle_PropagatesCancellationTokenToRepository()
    {
        var id = Guid.NewGuid();
        var readRepository = new Mock<IAccountCodeReadRepository>();
        using var cts = new CancellationTokenSource();
        var token = cts.Token;
        readRepository
            .Setup(r => r.GetByIdAsync(id, token))
            .ReturnsAsync((AccountCodeDto?)null);

        var handler = new GetAccountCodeByIdQueryHandler(readRepository.Object);

        await handler.Handle(new GetAccountCodeByIdQuery(id), token);

        readRepository.Verify(r => r.GetByIdAsync(id, token), Times.Once);
    }

    [Fact]
    public void Constructor_DoesNotDependOnIUnitOfWork()
    {
        // Read-side handlers must never touch IUnitOfWork — there is nothing to persist.
        var parameterTypes = typeof(GetAccountCodeByIdQueryHandler)
            .GetConstructors()
            .SelectMany(c => c.GetParameters())
            .Select(p => p.ParameterType);

        Assert.DoesNotContain(typeof(IUnitOfWork), parameterTypes);
    }
}
