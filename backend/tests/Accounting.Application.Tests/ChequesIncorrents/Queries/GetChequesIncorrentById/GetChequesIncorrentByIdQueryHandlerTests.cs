using Accounting.Application.ChequesIncorrents.Queries;
using Accounting.Application.ChequesIncorrents.Queries.GetChequesIncorrentById;
using Accounting.Application.Common.Interfaces;
using Moq;

namespace Accounting.Application.Tests.ChequesIncorrents.Queries.GetChequesIncorrentById;

public sealed class GetChequesIncorrentByIdQueryHandlerTests
{
    private static ChequesIncorrentDto SampleDto(Guid id) => new(
        Id: id,
        CheckId: Guid.NewGuid(),
        DocNum: "100001",
        DocDate: "13990101",
        CheqNo: "20000001",
        CheqDate: "13990102",
        PaperDesc: "بابت خرید کالا",
        PayTo: "شرکت الف",
        RecivDate: "13990110",
        AccountNumber: "1234567890123",
        Creditor: 500_000m,
        VahedCode: "0001",
        Year: "1399",
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
        var readRepository = new Mock<IChequesIncorrentReadRepository>();
        readRepository
            .Setup(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);

        var handler = new GetChequesIncorrentByIdQueryHandler(readRepository.Object);

        var result = await handler.Handle(new GetChequesIncorrentByIdQuery(id), CancellationToken.None);

        Assert.Same(expected, result);
    }

    [Fact]
    public async Task Handle_UnknownId_ReturnsNull_NotException()
    {
        var id = Guid.NewGuid();
        var readRepository = new Mock<IChequesIncorrentReadRepository>();
        readRepository
            .Setup(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync((ChequesIncorrentDto?)null);

        var handler = new GetChequesIncorrentByIdQueryHandler(readRepository.Object);

        var result = await handler.Handle(new GetChequesIncorrentByIdQuery(id), CancellationToken.None);

        Assert.Null(result);
    }

    [Fact]
    public async Task Handle_PropagatesCancellationTokenToRepository()
    {
        var id = Guid.NewGuid();
        var readRepository = new Mock<IChequesIncorrentReadRepository>();
        using var cts = new CancellationTokenSource();
        var token = cts.Token;
        readRepository
            .Setup(r => r.GetByIdAsync(id, token))
            .ReturnsAsync((ChequesIncorrentDto?)null);

        var handler = new GetChequesIncorrentByIdQueryHandler(readRepository.Object);

        await handler.Handle(new GetChequesIncorrentByIdQuery(id), token);

        readRepository.Verify(r => r.GetByIdAsync(id, token), Times.Once);
    }

    [Fact]
    public void Constructor_DoesNotDependOnIUnitOfWork()
    {
        var parameterTypes = typeof(GetChequesIncorrentByIdQueryHandler)
            .GetConstructors()
            .SelectMany(c => c.GetParameters())
            .Select(p => p.ParameterType);

        Assert.DoesNotContain(typeof(IUnitOfWork), parameterTypes);
    }
}
