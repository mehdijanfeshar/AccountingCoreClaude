using Accounting.Application.Common.Interfaces;
using Accounting.Application.TmpVoucherHeads.Queries;
using Accounting.Application.TmpVoucherHeads.Queries.GetTmpVoucherHeadById;
using Moq;

namespace Accounting.Application.Tests.TmpVoucherHeads.Queries.GetTmpVoucherHeadById;

public sealed class GetTmpVoucherHeadByIdQueryHandlerTests
{
    private static readonly Guid TargetId = Guid.Parse("77777777-7777-7777-7777-777777777777");

    private static TmpVoucherHeadDto SampleDto(bool? isDeleted = false) => new(
        Id: TargetId,
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
        IsDeleted: isDeleted);

    [Fact]
    public async Task Handle_ExistingRow_ReturnsDtoVerbatim()
    {
        var expected = SampleDto();
        var readRepository = new Mock<ITmpVoucherHeadReadRepository>();
        readRepository
            .Setup(r => r.GetByIdAsync(TargetId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);

        var handler = new GetTmpVoucherHeadByIdQueryHandler(readRepository.Object);

        var result = await handler.Handle(new GetTmpVoucherHeadByIdQuery(TargetId), CancellationToken.None);

        Assert.Same(expected, result);
    }

    [Fact]
    public async Task Handle_MissingRow_ReturnsNull()
    {
        var readRepository = new Mock<ITmpVoucherHeadReadRepository>();
        readRepository
            .Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((TmpVoucherHeadDto?)null);

        var handler = new GetTmpVoucherHeadByIdQueryHandler(readRepository.Object);

        var result = await handler.Handle(new GetTmpVoucherHeadByIdQuery(TargetId), CancellationToken.None);

        Assert.Null(result);
    }

    [Fact]
    public async Task Handle_SoftDeletedRow_IsStillReturned_WithIsDeletedTrue()
    {
        var readRepository = new Mock<ITmpVoucherHeadReadRepository>();
        readRepository
            .Setup(r => r.GetByIdAsync(TargetId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(SampleDto(isDeleted: true));

        var handler = new GetTmpVoucherHeadByIdQueryHandler(readRepository.Object);

        var result = await handler.Handle(new GetTmpVoucherHeadByIdQuery(TargetId), CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(true, result!.IsDeleted);
    }

    /// <summary>
    /// <c>IsDeleted</c> is <c>bool?</c> on this DTO, and a NULL row is "not deleted" — it must be
    /// surfaced as NULL rather than being normalised, so the caller sees exactly what Legacy
    /// stores.
    /// </summary>
    [Fact]
    public async Task Handle_NullIsDeleted_IsSurfacedAsNull_NotNormalised()
    {
        var readRepository = new Mock<ITmpVoucherHeadReadRepository>();
        readRepository
            .Setup(r => r.GetByIdAsync(TargetId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(SampleDto(isDeleted: null));

        var handler = new GetTmpVoucherHeadByIdQueryHandler(readRepository.Object);

        var result = await handler.Handle(new GetTmpVoucherHeadByIdQuery(TargetId), CancellationToken.None);

        Assert.NotNull(result);
        Assert.Null(result!.IsDeleted);
    }

    [Fact]
    public async Task Handle_PropagatesCancellationToken()
    {
        var readRepository = new Mock<ITmpVoucherHeadReadRepository>();
        readRepository
            .Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(SampleDto());
        using var cts = new CancellationTokenSource();

        var handler = new GetTmpVoucherHeadByIdQueryHandler(readRepository.Object);

        await handler.Handle(new GetTmpVoucherHeadByIdQuery(TargetId), cts.Token);

        readRepository.Verify(r => r.GetByIdAsync(TargetId, cts.Token), Times.Once);
    }
}
