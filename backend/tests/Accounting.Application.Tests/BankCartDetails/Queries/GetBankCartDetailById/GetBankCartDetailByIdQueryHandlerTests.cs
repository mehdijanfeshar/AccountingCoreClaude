using Accounting.Application.BankCartDetails.Queries;
using Accounting.Application.BankCartDetails.Queries.GetBankCartDetailById;
using Accounting.Application.Common.Interfaces;
using Moq;

namespace Accounting.Application.Tests.BankCartDetails.Queries.GetBankCartDetailById;

public sealed class GetBankCartDetailByIdQueryHandlerTests
{
    private static BankCartDetailDto SampleDto(Guid id) => new(
        Id: id,
        ReceipId: Guid.NewGuid(),
        CheckId: Guid.NewGuid(),
        BankId: Guid.NewGuid(),
        BranchId: Guid.NewGuid(),
        AccountNumber: "1234567890123",
        Month: "01",
        Cheqno: "12345678",
        RecivDate: "14020101",
        CheckReceiptType: true,
        Debtor: 1000m,
        Creditor: 0m,
        VahedCode: "0001",
        Year: "1402",
        CheckIncorrentId: Guid.NewGuid(),
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
        var readRepository = new Mock<IBankCartDetailReadRepository>();
        readRepository
            .Setup(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);

        var handler = new GetBankCartDetailByIdQueryHandler(readRepository.Object);

        var result = await handler.Handle(new GetBankCartDetailByIdQuery(id), CancellationToken.None);

        Assert.Same(expected, result);
    }

    [Fact]
    public async Task Handle_UnknownId_ReturnsNull_NotException()
    {
        var id = Guid.NewGuid();
        var readRepository = new Mock<IBankCartDetailReadRepository>();
        readRepository
            .Setup(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync((BankCartDetailDto?)null);

        var handler = new GetBankCartDetailByIdQueryHandler(readRepository.Object);

        var result = await handler.Handle(new GetBankCartDetailByIdQuery(id), CancellationToken.None);

        Assert.Null(result);
    }

    [Fact]
    public async Task Handle_PropagatesCancellationTokenToRepository()
    {
        var id = Guid.NewGuid();
        var readRepository = new Mock<IBankCartDetailReadRepository>();
        using var cts = new CancellationTokenSource();
        var token = cts.Token;
        readRepository
            .Setup(r => r.GetByIdAsync(id, token))
            .ReturnsAsync((BankCartDetailDto?)null);

        var handler = new GetBankCartDetailByIdQueryHandler(readRepository.Object);

        await handler.Handle(new GetBankCartDetailByIdQuery(id), token);

        readRepository.Verify(r => r.GetByIdAsync(id, token), Times.Once);
    }

    [Fact]
    public void Constructor_DoesNotDependOnIUnitOfWork()
    {
        var parameterTypes = typeof(GetBankCartDetailByIdQueryHandler)
            .GetConstructors()
            .SelectMany(c => c.GetParameters())
            .Select(p => p.ParameterType);

        Assert.DoesNotContain(typeof(IUnitOfWork), parameterTypes);
    }
}
