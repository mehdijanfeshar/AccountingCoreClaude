using Accounting.Application.Expenses.Queries;
using Accounting.Application.Expenses.Queries.GetExpenseById;
using Accounting.Application.Common.Interfaces;
using Moq;

namespace Accounting.Application.Tests.Expenses.Queries.GetExpenseById;

public sealed class GetExpenseByIdQueryHandlerTests
{
    private static ExpenseDto SampleDto(Guid id) => new(
        Id: id,
        ExpenseCode: "01",
        ExpenseName: "هزینه اداری",
        Description: "توضیحات نمونه",
        DefaultAmount: 5000m,
        ExpenseGroupId: Guid.NewGuid(),
        AccountCodeId: Guid.NewGuid(),
        VahedCode: "0001",
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
        var readRepository = new Mock<IExpenseReadRepository>();
        readRepository
            .Setup(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);

        var handler = new GetExpenseByIdQueryHandler(readRepository.Object);

        var result = await handler.Handle(new GetExpenseByIdQuery(id), CancellationToken.None);

        Assert.Same(expected, result);
    }

    [Fact]
    public async Task Handle_UnknownId_ReturnsNull_NotException()
    {
        var id = Guid.NewGuid();
        var readRepository = new Mock<IExpenseReadRepository>();
        readRepository
            .Setup(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync((ExpenseDto?)null);

        var handler = new GetExpenseByIdQueryHandler(readRepository.Object);

        var result = await handler.Handle(new GetExpenseByIdQuery(id), CancellationToken.None);

        Assert.Null(result);
    }

    [Fact]
    public async Task Handle_PropagatesCancellationTokenToRepository()
    {
        var id = Guid.NewGuid();
        var readRepository = new Mock<IExpenseReadRepository>();
        using var cts = new CancellationTokenSource();
        var token = cts.Token;
        readRepository
            .Setup(r => r.GetByIdAsync(id, token))
            .ReturnsAsync((ExpenseDto?)null);

        var handler = new GetExpenseByIdQueryHandler(readRepository.Object);

        await handler.Handle(new GetExpenseByIdQuery(id), token);

        readRepository.Verify(r => r.GetByIdAsync(id, token), Times.Once);
    }

    [Fact]
    public void Constructor_DoesNotDependOnIUnitOfWork()
    {
        var parameterTypes = typeof(GetExpenseByIdQueryHandler)
            .GetConstructors()
            .SelectMany(c => c.GetParameters())
            .Select(p => p.ParameterType);

        Assert.DoesNotContain(typeof(IUnitOfWork), parameterTypes);
    }
}
