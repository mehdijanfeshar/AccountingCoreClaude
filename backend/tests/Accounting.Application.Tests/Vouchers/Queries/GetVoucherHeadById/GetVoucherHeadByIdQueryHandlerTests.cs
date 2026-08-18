using Accounting.Application.Common.Interfaces;
using Accounting.Application.Vouchers.Queries;
using Accounting.Application.Vouchers.Queries.GetVoucherHeadById;
using Moq;

namespace Accounting.Application.Tests.Vouchers.Queries.GetVoucherHeadById;

public sealed class GetVoucherHeadByIdQueryHandlerTests
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
    public async Task Handle_ExistingId_ReturnsRepositoryDto()
    {
        var id = Guid.NewGuid();
        var expected = SampleDto(id);
        var readRepository = new Mock<IVoucherHeadReadRepository>();
        readRepository
            .Setup(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);

        var handler = new GetVoucherHeadByIdQueryHandler(readRepository.Object);

        var result = await handler.Handle(new GetVoucherHeadByIdQuery(id), CancellationToken.None);

        Assert.Same(expected, result);
    }

    [Fact]
    public async Task Handle_UnknownId_ReturnsNull_NotException()
    {
        var id = Guid.NewGuid();
        var readRepository = new Mock<IVoucherHeadReadRepository>();
        readRepository
            .Setup(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync((VoucherHeadDto?)null);

        var handler = new GetVoucherHeadByIdQueryHandler(readRepository.Object);

        var result = await handler.Handle(new GetVoucherHeadByIdQuery(id), CancellationToken.None);

        Assert.Null(result);
    }

    [Fact]
    public async Task Handle_PropagatesCancellationTokenToRepository()
    {
        var id = Guid.NewGuid();
        var readRepository = new Mock<IVoucherHeadReadRepository>();
        using var cts = new CancellationTokenSource();
        var token = cts.Token;
        readRepository
            .Setup(r => r.GetByIdAsync(id, token))
            .ReturnsAsync((VoucherHeadDto?)null);

        var handler = new GetVoucherHeadByIdQueryHandler(readRepository.Object);

        await handler.Handle(new GetVoucherHeadByIdQuery(id), token);

        readRepository.Verify(r => r.GetByIdAsync(id, token), Times.Once);
    }

    [Fact]
    public void Constructor_DoesNotDependOnIUnitOfWork()
    {
        // Read-side handlers must never touch IUnitOfWork — there is nothing to persist.
        var parameterTypes = typeof(GetVoucherHeadByIdQueryHandler)
            .GetConstructors()
            .SelectMany(c => c.GetParameters())
            .Select(p => p.ParameterType);

        Assert.DoesNotContain(typeof(IUnitOfWork), parameterTypes);
    }
}
