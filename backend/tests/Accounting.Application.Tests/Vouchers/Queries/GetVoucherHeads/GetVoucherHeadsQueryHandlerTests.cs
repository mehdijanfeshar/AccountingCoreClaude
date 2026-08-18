using Accounting.Application.Common;
using Accounting.Application.Common.Interfaces;
using Accounting.Application.Vouchers.Queries;
using Accounting.Application.Vouchers.Queries.GetVoucherHeads;
using Moq;

namespace Accounting.Application.Tests.Vouchers.Queries.GetVoucherHeads;

public sealed class GetVoucherHeadsQueryHandlerTests
{
    private static VoucherHeadDto SampleDto(Guid id) => new(
        Id: id,
        DocNum: "000001",
        DateDoc: "14050101",
        DocLife: true,
        HeadDesc: "سند افتتاحیه",
        Apendix: null,
        SystemTypeId: null,
        FlagState: null,
        CreatedDate: DateTime.UtcNow,
        UpdatedDate: null,
        AddUserId: "user1",
        ChangeUserId: null,
        VahedCode: "0001",
        Year: "1405",
        IsDeleted: false,
        AttachFileName: null,
        AtfNum: null,
        IsAutomatic: false,
        SndVahedCode: null,
        ParentHeadId: null,
        GlobalNumber: null);

    [Fact]
    public async Task Handle_PassesPagingAndFiltersToRepository_AndReturnsRepositoryResult()
    {
        var readRepository = new Mock<IVoucherHeadReadRepository>();
        var expected = new PagedResult<VoucherHeadDto>
        {
            Items = new[] { SampleDto(Guid.NewGuid()) },
            PageNumber = 3,
            PageSize = 10,
            TotalCount = 42,
        };
        readRepository
            .Setup(r => r.GetPagedAsync(3, 10, "1405", "0001", It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);

        var handler = new GetVoucherHeadsQueryHandler(readRepository.Object);

        var result = await handler.Handle(
            new GetVoucherHeadsQuery(PageNumber: 3, PageSize: 10, Year: "1405", VahedCode: "0001"),
            CancellationToken.None);

        Assert.Same(expected, result);
        Assert.Equal(3, result.PageNumber);
        Assert.Equal(10, result.PageSize);
        Assert.Equal(42, result.TotalCount);
        readRepository.Verify(r => r.GetPagedAsync(3, 10, "1405", "0001", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_NullFilters_PassedThroughAsNull()
    {
        var readRepository = new Mock<IVoucherHeadReadRepository>();
        readRepository
            .Setup(r => r.GetPagedAsync(1, 20, null, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PagedResult<VoucherHeadDto>());

        var handler = new GetVoucherHeadsQueryHandler(readRepository.Object);

        await handler.Handle(new GetVoucherHeadsQuery(PageNumber: 1, PageSize: 20, Year: null, VahedCode: null), CancellationToken.None);

        readRepository.Verify(r => r.GetPagedAsync(1, 20, null, null, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_PropagatesCancellationTokenToRepository()
    {
        var readRepository = new Mock<IVoucherHeadReadRepository>();
        using var cts = new CancellationTokenSource();
        var token = cts.Token;
        readRepository
            .Setup(r => r.GetPagedAsync(1, 20, null, null, token))
            .ReturnsAsync(new PagedResult<VoucherHeadDto>());

        var handler = new GetVoucherHeadsQueryHandler(readRepository.Object);

        await handler.Handle(new GetVoucherHeadsQuery(PageNumber: 1, PageSize: 20, Year: null, VahedCode: null), token);

        readRepository.Verify(r => r.GetPagedAsync(1, 20, null, null, token), Times.Once);
    }

    [Fact]
    public void Constructor_DoesNotDependOnIUnitOfWork()
    {
        // Read-side handlers must never touch IUnitOfWork — there is nothing to persist.
        var parameterTypes = typeof(GetVoucherHeadsQueryHandler)
            .GetConstructors()
            .SelectMany(c => c.GetParameters())
            .Select(p => p.ParameterType);

        Assert.DoesNotContain(typeof(IUnitOfWork), parameterTypes);
    }
}
