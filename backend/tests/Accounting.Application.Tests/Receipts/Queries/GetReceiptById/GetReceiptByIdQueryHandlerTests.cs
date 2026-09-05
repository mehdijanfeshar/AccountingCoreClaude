using Accounting.Application.Receipts.Queries;
using Accounting.Application.Receipts.Queries.GetReceiptById;
using Accounting.Application.Common.Interfaces;
using Moq;

namespace Accounting.Application.Tests.Receipts.Queries.GetReceiptById;

public sealed class GetReceiptByIdQueryHandlerTests
{
    private static ReceiptDto SampleDto(Guid id) => new(
        Id: id,
        ReceiptKind: true,
        ReceiptDate: "14020101",
        ReceiptNo: "R0000001",
        DateRsid: "14020102",
        VahedCode: "0001",
        Year: "1402",
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
        var readRepository = new Mock<IReceiptReadRepository>();
        readRepository
            .Setup(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);

        var handler = new GetReceiptByIdQueryHandler(readRepository.Object);

        var result = await handler.Handle(new GetReceiptByIdQuery(id), CancellationToken.None);

        Assert.Same(expected, result);
    }

    [Fact]
    public async Task Handle_UnknownId_ReturnsNull_NotException()
    {
        var id = Guid.NewGuid();
        var readRepository = new Mock<IReceiptReadRepository>();
        readRepository
            .Setup(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync((ReceiptDto?)null);

        var handler = new GetReceiptByIdQueryHandler(readRepository.Object);

        var result = await handler.Handle(new GetReceiptByIdQuery(id), CancellationToken.None);

        Assert.Null(result);
    }

    [Fact]
    public async Task Handle_PropagatesCancellationTokenToRepository()
    {
        var id = Guid.NewGuid();
        var readRepository = new Mock<IReceiptReadRepository>();
        using var cts = new CancellationTokenSource();
        var token = cts.Token;
        readRepository
            .Setup(r => r.GetByIdAsync(id, token))
            .ReturnsAsync((ReceiptDto?)null);

        var handler = new GetReceiptByIdQueryHandler(readRepository.Object);

        await handler.Handle(new GetReceiptByIdQuery(id), token);

        readRepository.Verify(r => r.GetByIdAsync(id, token), Times.Once);
    }

    [Fact]
    public void Constructor_DoesNotDependOnIUnitOfWork()
    {
        var parameterTypes = typeof(GetReceiptByIdQueryHandler)
            .GetConstructors()
            .SelectMany(c => c.GetParameters())
            .Select(p => p.ParameterType);

        Assert.DoesNotContain(typeof(IUnitOfWork), parameterTypes);
    }
}
