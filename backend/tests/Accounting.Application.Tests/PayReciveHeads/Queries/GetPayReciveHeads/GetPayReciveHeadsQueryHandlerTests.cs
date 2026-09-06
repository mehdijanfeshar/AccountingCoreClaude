using Accounting.Application.Common;
using Accounting.Application.Common.Interfaces;
using Accounting.Application.PayReciveHeads.Queries;
using Accounting.Application.PayReciveHeads.Queries.GetPayReciveHeads;
using Moq;

namespace Accounting.Application.Tests.PayReciveHeads.Queries.GetPayReciveHeads;

public sealed class GetPayReciveHeadsQueryHandlerTests
{
    private static PayReciveHeadDto SampleDto() => new(
        Id: Guid.NewGuid(),
        PayReciveCode: "00123",
        PayReciveDate: "14040101",
        PayReciveDescription: "شرح سند",
        PayReciveType: true,
        VahedCode: "0001",
        Year: "1404",
        VoucherHeadId: null,
        CreatedDate: DateTime.UtcNow,
        UpdatedDate: null,
        AddUserId: "creator01",
        ChangeUserId: null,
        IsDeleted: false);

    [Fact]
    public async Task Handle_PassesPagingArgumentsThroughUnchanged()
    {
        var readRepository = new Mock<IPayReciveHeadReadRepository>();
        readRepository
            .Setup(r => r.GetPagedAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PagedResult<PayReciveHeadDto>());

        var handler = new GetPayReciveHeadsQueryHandler(readRepository.Object);

        await handler.Handle(new GetPayReciveHeadsQuery(3, 50), CancellationToken.None);

        readRepository.Verify(r => r.GetPagedAsync(3, 50, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_ReturnsRepositoryResultVerbatim()
    {
        var expected = new PagedResult<PayReciveHeadDto>
        {
            Items = new[] { SampleDto() },
            PageNumber = 2,
            PageSize = 20,
            TotalCount = 41,
        };
        var readRepository = new Mock<IPayReciveHeadReadRepository>();
        readRepository
            .Setup(r => r.GetPagedAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);

        var handler = new GetPayReciveHeadsQueryHandler(readRepository.Object);

        var result = await handler.Handle(new GetPayReciveHeadsQuery(2, 20), CancellationToken.None);

        Assert.Same(expected, result);
        Assert.Equal(41, result.TotalCount);
        Assert.Single(result.Items);
    }

    [Fact]
    public async Task Handle_PropagatesCancellationToken()
    {
        var readRepository = new Mock<IPayReciveHeadReadRepository>();
        readRepository
            .Setup(r => r.GetPagedAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PagedResult<PayReciveHeadDto>());
        using var cts = new CancellationTokenSource();

        var handler = new GetPayReciveHeadsQueryHandler(readRepository.Object);

        await handler.Handle(new GetPayReciveHeadsQuery(1, 20), cts.Token);

        readRepository.Verify(r => r.GetPagedAsync(1, 20, cts.Token), Times.Once);
    }
}
