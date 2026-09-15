using Accounting.Application.Common.Interfaces;
using Accounting.Application.PayReciveHeads.Queries;
using Accounting.Application.PayReciveHeads.Queries.GetPayReciveHeadById;
using Moq;

namespace Accounting.Application.Tests.PayReciveHeads.Queries.GetPayReciveHeadById;

public sealed class GetPayReciveHeadByIdQueryHandlerTests
{
    private static readonly Guid TargetId = Guid.Parse("44444444-4444-4444-4444-444444444444");

    private static PayReciveHeadDto SampleDto(bool isDeleted = false) => new(
        Id: TargetId,
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
        IsDeleted: isDeleted);

    [Fact]
    public async Task Handle_ExistingRow_ReturnsDtoVerbatim()
    {
        var expected = SampleDto();
        var readRepository = new Mock<IPayReciveHeadReadRepository>();
        readRepository
            .Setup(r => r.GetByIdAsync(TargetId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);

        var handler = new GetPayReciveHeadByIdQueryHandler(readRepository.Object);

        var result = await handler.Handle(new GetPayReciveHeadByIdQuery(TargetId), CancellationToken.None);

        Assert.Same(expected, result);
    }

    [Fact]
    public async Task Handle_MissingRow_ReturnsNull()
    {
        var readRepository = new Mock<IPayReciveHeadReadRepository>();
        readRepository
            .Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((PayReciveHeadDto?)null);

        var handler = new GetPayReciveHeadByIdQueryHandler(readRepository.Object);

        var result = await handler.Handle(new GetPayReciveHeadByIdQuery(TargetId), CancellationToken.None);

        Assert.Null(result);
    }

    /// <summary>
    /// GetById deliberately does NOT filter soft-deleted rows — it returns them with
    /// <c>IsDeleted = true</c> so the caller can tell "deleted" apart from "never existed".
    /// </summary>
    [Fact]
    public async Task Handle_SoftDeletedRow_IsStillReturned_WithIsDeletedTrue()
    {
        var readRepository = new Mock<IPayReciveHeadReadRepository>();
        readRepository
            .Setup(r => r.GetByIdAsync(TargetId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(SampleDto(isDeleted: true));

        var handler = new GetPayReciveHeadByIdQueryHandler(readRepository.Object);

        var result = await handler.Handle(new GetPayReciveHeadByIdQuery(TargetId), CancellationToken.None);

        Assert.NotNull(result);
        Assert.True(result!.IsDeleted);
    }

    [Fact]
    public async Task Handle_PropagatesCancellationToken()
    {
        var readRepository = new Mock<IPayReciveHeadReadRepository>();
        readRepository
            .Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(SampleDto());
        using var cts = new CancellationTokenSource();

        var handler = new GetPayReciveHeadByIdQueryHandler(readRepository.Object);

        await handler.Handle(new GetPayReciveHeadByIdQuery(TargetId), cts.Token);

        readRepository.Verify(r => r.GetByIdAsync(TargetId, cts.Token), Times.Once);
    }
}
