using Accounting.Application.ElamHeads.Queries;
using Accounting.Application.ElamHeads.Queries.GetElamHeads;
using Accounting.Application.Common;
using Accounting.Application.Common.Interfaces;
using Moq;

namespace Accounting.Application.Tests.ElamHeads.Queries.GetElamHeads;

public sealed class GetElamHeadsQueryHandlerTests
{
    private static ElamHeadDto SampleDto(Guid id) => new(
        Id: id,
        VoucherHeadId: Guid.NewGuid(),
        SerialNo: "SER-001",
        Code: "COD-01",
        DabirNo: null,
        DabirDate: null,
        PrintNo: null,
        Case: true,
        SerialNoInput: null,
        WebStat: 2,
        Date: null,
        Desc: null,
        WorkShopId: null,
        RcvNo: null,
        RcvDt: null,
        LstMon: null,
        PayNo: null,
        DramadType: false,
        PeimanNo: null,
        WorkShopCode: null,
        WorkShopName: null,
        SendRcvVahed: null,
        ElamYear: "04",
        VahedCode: "0001",
        Year: "1404",
        ElamSenderId: null,
        CreatedDate: DateTime.UtcNow,
        UpdatedDate: null,
        AddUserId: "user1",
        ChangeUserId: null,
        IsDeleted: false);

    [Fact]
    public async Task Handle_PassesPageNumberAndPageSizeToRepository_AndReturnsRepositoryResult()
    {
        var readRepository = new Mock<IElamHeadReadRepository>();
        var expected = new PagedResult<ElamHeadDto>
        {
            Items = new[] { SampleDto(Guid.NewGuid()) },
            PageNumber = 2,
            PageSize = 25,
            TotalCount = 51,
        };
        readRepository
            .Setup(r => r.GetPagedAsync(2, 25, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);

        var handler = new GetElamHeadsQueryHandler(readRepository.Object);

        var result = await handler.Handle(new GetElamHeadsQuery(PageNumber: 2, PageSize: 25), CancellationToken.None);

        Assert.Same(expected, result);
        Assert.Equal(2, result.PageNumber);
        Assert.Equal(25, result.PageSize);
        Assert.Equal(51, result.TotalCount);
        readRepository.Verify(r => r.GetPagedAsync(2, 25, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_PropagatesCancellationTokenToRepository()
    {
        var readRepository = new Mock<IElamHeadReadRepository>();
        using var cts = new CancellationTokenSource();
        var token = cts.Token;
        readRepository
            .Setup(r => r.GetPagedAsync(1, 20, token))
            .ReturnsAsync(new PagedResult<ElamHeadDto>());

        var handler = new GetElamHeadsQueryHandler(readRepository.Object);

        await handler.Handle(new GetElamHeadsQuery(PageNumber: 1, PageSize: 20), token);

        readRepository.Verify(r => r.GetPagedAsync(1, 20, token), Times.Once);
    }

    [Fact]
    public void Constructor_DoesNotDependOnIUnitOfWork()
    {
        var parameterTypes = typeof(GetElamHeadsQueryHandler)
            .GetConstructors()
            .SelectMany(c => c.GetParameters())
            .Select(p => p.ParameterType);

        Assert.DoesNotContain(typeof(IUnitOfWork), parameterTypes);
    }
}
