using Accounting.Application.BankAccounts.Queries;
using Accounting.Application.BankAccounts.Queries.GetBankAccountById;
using Accounting.Application.Common.Interfaces;
using Moq;

namespace Accounting.Application.Tests.BankAccounts.Queries.GetBankAccountById;

public sealed class GetBankAccountByIdQueryHandlerTests
{
    private static BankAccountDto SampleDto(Guid id) => new(
        Id: id,
        AccountNumber: "1234567890",
        AccountHolder: "علی رضایی",
        CardNumber: "6037991234567890",
        ShebaNumber: "IR120170000000123456789012",
        FirstAmount: 1_000_000m,
        BankId: Guid.NewGuid(),
        BranchId: Guid.NewGuid(),
        AccountTypeId: Guid.NewGuid(),
        AccountCodeId: Guid.NewGuid(),
        VahedCode: "0001",
        AccountOpeningDate: "13990101",
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
        var readRepository = new Mock<IBankAccountReadRepository>();
        readRepository
            .Setup(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);

        var handler = new GetBankAccountByIdQueryHandler(readRepository.Object);

        var result = await handler.Handle(new GetBankAccountByIdQuery(id), CancellationToken.None);

        Assert.Same(expected, result);
    }

    [Fact]
    public async Task Handle_UnknownId_ReturnsNull_NotException()
    {
        var id = Guid.NewGuid();
        var readRepository = new Mock<IBankAccountReadRepository>();
        readRepository
            .Setup(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync((BankAccountDto?)null);

        var handler = new GetBankAccountByIdQueryHandler(readRepository.Object);

        var result = await handler.Handle(new GetBankAccountByIdQuery(id), CancellationToken.None);

        Assert.Null(result);
    }

    [Fact]
    public async Task Handle_PropagatesCancellationTokenToRepository()
    {
        var id = Guid.NewGuid();
        var readRepository = new Mock<IBankAccountReadRepository>();
        using var cts = new CancellationTokenSource();
        var token = cts.Token;
        readRepository
            .Setup(r => r.GetByIdAsync(id, token))
            .ReturnsAsync((BankAccountDto?)null);

        var handler = new GetBankAccountByIdQueryHandler(readRepository.Object);

        await handler.Handle(new GetBankAccountByIdQuery(id), token);

        readRepository.Verify(r => r.GetByIdAsync(id, token), Times.Once);
    }

    [Fact]
    public void Constructor_DoesNotDependOnIUnitOfWork()
    {
        var parameterTypes = typeof(GetBankAccountByIdQueryHandler)
            .GetConstructors()
            .SelectMany(c => c.GetParameters())
            .Select(p => p.ParameterType);

        Assert.DoesNotContain(typeof(IUnitOfWork), parameterTypes);
    }
}
