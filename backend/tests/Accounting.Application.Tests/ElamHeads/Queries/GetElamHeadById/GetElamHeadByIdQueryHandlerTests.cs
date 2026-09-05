using Accounting.Application.ElamHeads.Queries;
using Accounting.Application.ElamHeads.Queries.GetElamHeadById;
using Accounting.Application.Common.Interfaces;
using Moq;

namespace Accounting.Application.Tests.ElamHeads.Queries.GetElamHeadById;

public sealed class GetElamHeadByIdQueryHandlerTests
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
    public async Task Handle_ExistingId_ReturnsRepositoryDto()
    {
        var id = Guid.NewGuid();
        var expected = SampleDto(id);
        var readRepository = new Mock<IElamHeadReadRepository>();
        readRepository
            .Setup(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);

        var handler = new GetElamHeadByIdQueryHandler(readRepository.Object);

        var result = await handler.Handle(new GetElamHeadByIdQuery(id), CancellationToken.None);

        Assert.Same(expected, result);
    }

    [Fact]
    public async Task Handle_UnknownId_ReturnsNull_NotException()
    {
        var id = Guid.NewGuid();
        var readRepository = new Mock<IElamHeadReadRepository>();
        readRepository
            .Setup(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync((ElamHeadDto?)null);

        var handler = new GetElamHeadByIdQueryHandler(readRepository.Object);

        var result = await handler.Handle(new GetElamHeadByIdQuery(id), CancellationToken.None);

        Assert.Null(result);
    }

    [Fact]
    public async Task Handle_PropagatesCancellationTokenToRepository()
    {
        var id = Guid.NewGuid();
        var readRepository = new Mock<IElamHeadReadRepository>();
        using var cts = new CancellationTokenSource();
        var token = cts.Token;
        readRepository
            .Setup(r => r.GetByIdAsync(id, token))
            .ReturnsAsync((ElamHeadDto?)null);

        var handler = new GetElamHeadByIdQueryHandler(readRepository.Object);

        await handler.Handle(new GetElamHeadByIdQuery(id), token);

        readRepository.Verify(r => r.GetByIdAsync(id, token), Times.Once);
    }

    [Fact]
    public void Constructor_DoesNotDependOnIUnitOfWork()
    {
        var parameterTypes = typeof(GetElamHeadByIdQueryHandler)
            .GetConstructors()
            .SelectMany(c => c.GetParameters())
            .Select(p => p.ParameterType);

        Assert.DoesNotContain(typeof(IUnitOfWork), parameterTypes);
    }
}
