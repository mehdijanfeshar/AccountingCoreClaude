using Accounting.Application.BankCartDetails.Queries;
using Accounting.Application.BankCartDetails.Queries.GetBankCartDetails;
using Accounting.Application.Common;
using Accounting.Application.Common.Interfaces;
using Moq;

namespace Accounting.Application.Tests.BankCartDetails.Queries.GetBankCartDetails;

public sealed class GetBankCartDetailsQueryHandlerTests
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
    public async Task Handle_PassesPageNumberAndPageSizeToRepository_AndReturnsRepositoryResult()
    {
        var readRepository = new Mock<IBankCartDetailReadRepository>();
        var expected = new PagedResult<BankCartDetailDto>
        {
            Items = new[] { SampleDto(Guid.NewGuid()) },
            PageNumber = 2,
            PageSize = 25,
            TotalCount = 51,
        };
        readRepository
            .Setup(r => r.GetPagedAsync(2, 25, It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);

        var handler = new GetBankCartDetailsQueryHandler(readRepository.Object);
        var query = new GetBankCartDetailsQuery(PageNumber: 2, PageSize: 25) { VahedCode = "0001" };

        var result = await handler.Handle(query, CancellationToken.None);

        Assert.Same(expected, result);
        Assert.Equal(2, result.PageNumber);
        Assert.Equal(25, result.PageSize);
        Assert.Equal(51, result.TotalCount);
        readRepository.Verify(r => r.GetPagedAsync(2, 25, It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_PassesRequestVahedCodeToRepository_AtFaceValue()
    {
        // Proves the handler trusts request.VahedCode as-is: by the time this handler runs,
        // VahedScopeBehavior has already overwritten it with the authenticated caller's own
        // unit code, so the handler must forward exactly that value, not derive its own.
        var readRepository = new Mock<IBankCartDetailReadRepository>();
        readRepository
            .Setup(r => r.GetPagedAsync(It.IsAny<int>(), It.IsAny<int>(), "0007", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PagedResult<BankCartDetailDto>());

        var handler = new GetBankCartDetailsQueryHandler(readRepository.Object);
        var query = new GetBankCartDetailsQuery(PageNumber: 1, PageSize: 20) { VahedCode = "0007" };

        await handler.Handle(query, CancellationToken.None);

        readRepository.Verify(
            r => r.GetPagedAsync(1, 20, "0007", It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_PropagatesCancellationTokenToRepository()
    {
        var readRepository = new Mock<IBankCartDetailReadRepository>();
        using var cts = new CancellationTokenSource();
        var token = cts.Token;
        readRepository
            .Setup(r => r.GetPagedAsync(1, 20, "0001", token))
            .ReturnsAsync(new PagedResult<BankCartDetailDto>());

        var handler = new GetBankCartDetailsQueryHandler(readRepository.Object);
        var query = new GetBankCartDetailsQuery(PageNumber: 1, PageSize: 20) { VahedCode = "0001" };

        await handler.Handle(query, token);

        readRepository.Verify(r => r.GetPagedAsync(1, 20, "0001", token), Times.Once);
    }

    [Fact]
    public void Constructor_DoesNotDependOnIUnitOfWork()
    {
        var parameterTypes = typeof(GetBankCartDetailsQueryHandler)
            .GetConstructors()
            .SelectMany(c => c.GetParameters())
            .Select(p => p.ParameterType);

        Assert.DoesNotContain(typeof(IUnitOfWork), parameterTypes);
    }
}
