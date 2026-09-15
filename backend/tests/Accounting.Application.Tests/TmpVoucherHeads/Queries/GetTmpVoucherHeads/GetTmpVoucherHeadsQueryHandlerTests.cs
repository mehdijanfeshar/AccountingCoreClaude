using Accounting.Application.Common;
using Accounting.Application.Common.Interfaces;
using Accounting.Application.TmpVoucherHeads.Queries;
using Accounting.Application.TmpVoucherHeads.Queries.GetTmpVoucherHeads;
using Moq;

namespace Accounting.Application.Tests.TmpVoucherHeads.Queries.GetTmpVoucherHeads;

public sealed class GetTmpVoucherHeadsQueryHandlerTests
{
    private static TmpVoucherHeadDto SampleDto() => new(
        Id: Guid.NewGuid(),
        VoucherHeadId: null,
        DateDoc: "14040101",
        HeadDesc: "شرح سند موقت",
        VahedCode: "0001",
        Year: "1404",
        SysType: "K",
        SourceId: null,
        CreatedDate: DateTime.UtcNow,
        UpdatedDate: null,
        AddUserId: "creator02",
        ChangeUserId: null,
        IsDeleted: false);

    [Fact]
    public async Task Handle_PassesPagingArgumentsThroughUnchanged()
    {
        var readRepository = new Mock<ITmpVoucherHeadReadRepository>();
        readRepository
            .Setup(r => r.GetPagedAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PagedResult<TmpVoucherHeadDto>());

        var handler = new GetTmpVoucherHeadsQueryHandler(readRepository.Object);
        var query = new GetTmpVoucherHeadsQuery(4, 75) { VahedCode = "0001" };

        await handler.Handle(query, CancellationToken.None);

        readRepository.Verify(r => r.GetPagedAsync(4, 75, It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_PassesRequestVahedCodeToRepository_AtFaceValue()
    {
        // Proves the handler trusts request.VahedCode as-is: by the time this handler runs,
        // VahedScopeBehavior has already overwritten it with the authenticated caller's own
        // unit code, so the handler must forward exactly that value, not derive its own.
        var readRepository = new Mock<ITmpVoucherHeadReadRepository>();
        readRepository
            .Setup(r => r.GetPagedAsync(It.IsAny<int>(), It.IsAny<int>(), "0007", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PagedResult<TmpVoucherHeadDto>());

        var handler = new GetTmpVoucherHeadsQueryHandler(readRepository.Object);
        var query = new GetTmpVoucherHeadsQuery(1, 20) { VahedCode = "0007" };

        await handler.Handle(query, CancellationToken.None);

        readRepository.Verify(
            r => r.GetPagedAsync(1, 20, "0007", It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_ReturnsRepositoryResultVerbatim()
    {
        var expected = new PagedResult<TmpVoucherHeadDto>
        {
            Items = new[] { SampleDto() },
            PageNumber = 1,
            PageSize = 20,
            TotalCount = 7,
        };
        var readRepository = new Mock<ITmpVoucherHeadReadRepository>();
        readRepository
            .Setup(r => r.GetPagedAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);

        var handler = new GetTmpVoucherHeadsQueryHandler(readRepository.Object);
        var query = new GetTmpVoucherHeadsQuery(1, 20) { VahedCode = "0001" };

        var result = await handler.Handle(query, CancellationToken.None);

        Assert.Same(expected, result);
        Assert.Equal(7, result.TotalCount);
        Assert.Single(result.Items);
    }

    [Fact]
    public async Task Handle_PropagatesCancellationToken()
    {
        var readRepository = new Mock<ITmpVoucherHeadReadRepository>();
        readRepository
            .Setup(r => r.GetPagedAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PagedResult<TmpVoucherHeadDto>());
        using var cts = new CancellationTokenSource();

        var handler = new GetTmpVoucherHeadsQueryHandler(readRepository.Object);
        var query = new GetTmpVoucherHeadsQuery(1, 20) { VahedCode = "0001" };

        await handler.Handle(query, cts.Token);

        readRepository.Verify(r => r.GetPagedAsync(1, 20, "0001", cts.Token), Times.Once);
    }

    /// <summary>
    /// Pins the Head-only read scope: the DTO carries no detail collection, so a temporary
    /// voucher's staged lines are invisible from this API — matching the write side, which
    /// cannot create them either.
    /// </summary>
    [Fact]
    public void Dto_ExposesNoDetailCollection()
    {
        var hasCollectionProperty = typeof(TmpVoucherHeadDto)
            .GetProperties()
            .Any(p => p.PropertyType != typeof(string)
                && typeof(System.Collections.IEnumerable).IsAssignableFrom(p.PropertyType));

        Assert.False(hasCollectionProperty);
    }
}
