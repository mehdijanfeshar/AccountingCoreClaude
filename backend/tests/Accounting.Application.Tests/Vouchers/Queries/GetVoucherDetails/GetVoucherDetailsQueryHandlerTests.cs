using Accounting.Application.Common;
using Accounting.Application.Common.Interfaces;
using Accounting.Application.Vouchers.Queries;
using Accounting.Application.Vouchers.Queries.GetVoucherDetails;
using Moq;

namespace Accounting.Application.Tests.Vouchers.Queries.GetVoucherDetails;

public sealed class GetVoucherDetailsQueryHandlerTests
{
    private static VoucherDetailDto SampleDto(Guid id) => new(
        Id: id,
        VoucherHeadId: Guid.NewGuid(),
        AccountId: Guid.NewGuid(),
        ReceiptId: null,
        CheckId: null,
        LowLevelCodeId: null,
        EtebarId: null,
        Description: "ردیف",
        Radif: 1,
        Debtor: 1000m,
        Creditor: null,
        CreatedDate: DateTime.UtcNow,
        UpdatedDate: null,
        AddUserId: "user1",
        ChangeUserId: null,
        VahedCode: "0001",
        Year: "1405",
        IsDeleted: false);

    [Fact]
    public async Task Handle_PassesPagingAndFiltersToRepository_AndReturnsRepositoryResult()
    {
        var readRepository = new Mock<IVoucherDetailReadRepository>();
        var voucherHeadId = Guid.NewGuid();
        var expected = new PagedResult<VoucherDetailDto>
        {
            Items = new[] { SampleDto(Guid.NewGuid()) },
            PageNumber = 3,
            PageSize = 10,
            TotalCount = 42,
        };
        readRepository
            .Setup(r => r.GetPagedAsync(3, 10, voucherHeadId, "1405", "0001", It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);

        var handler = new GetVoucherDetailsQueryHandler(readRepository.Object);

        var result = await handler.Handle(
            new GetVoucherDetailsQuery(PageNumber: 3, PageSize: 10, VoucherHeadId: voucherHeadId, Year: "1405", VahedCode: "0001"),
            CancellationToken.None);

        Assert.Same(expected, result);
        Assert.Equal(3, result.PageNumber);
        Assert.Equal(10, result.PageSize);
        Assert.Equal(42, result.TotalCount);
        readRepository.Verify(
            r => r.GetPagedAsync(3, 10, voucherHeadId, "1405", "0001", It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_NullFilters_PassedThroughAsNull()
    {
        var readRepository = new Mock<IVoucherDetailReadRepository>();
        readRepository
            .Setup(r => r.GetPagedAsync(1, 20, null, null, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PagedResult<VoucherDetailDto>());

        var handler = new GetVoucherDetailsQueryHandler(readRepository.Object);

        await handler.Handle(
            new GetVoucherDetailsQuery(PageNumber: 1, PageSize: 20, VoucherHeadId: null, Year: null, VahedCode: null),
            CancellationToken.None);

        readRepository.Verify(r => r.GetPagedAsync(1, 20, null, null, null, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_PropagatesCancellationTokenToRepository()
    {
        var readRepository = new Mock<IVoucherDetailReadRepository>();
        using var cts = new CancellationTokenSource();
        var token = cts.Token;
        readRepository
            .Setup(r => r.GetPagedAsync(1, 20, null, null, null, token))
            .ReturnsAsync(new PagedResult<VoucherDetailDto>());

        var handler = new GetVoucherDetailsQueryHandler(readRepository.Object);

        await handler.Handle(
            new GetVoucherDetailsQuery(PageNumber: 1, PageSize: 20, VoucherHeadId: null, Year: null, VahedCode: null),
            token);

        readRepository.Verify(r => r.GetPagedAsync(1, 20, null, null, null, token), Times.Once);
    }

    [Fact]
    public void Constructor_DoesNotDependOnIUnitOfWork()
    {
        var parameterTypes = typeof(GetVoucherDetailsQueryHandler)
            .GetConstructors()
            .SelectMany(c => c.GetParameters())
            .Select(p => p.ParameterType);

        Assert.DoesNotContain(typeof(IUnitOfWork), parameterTypes);
    }
}
