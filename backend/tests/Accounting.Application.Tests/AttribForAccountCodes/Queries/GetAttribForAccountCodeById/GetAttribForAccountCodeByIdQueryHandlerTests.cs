using Accounting.Application.AttribForAccountCodes.Queries.GetAttribForAccountCodes;
using Accounting.Application.AttribForAccountCodes.Queries;
using Accounting.Application.AttribForAccountCodes.Queries.GetAttribForAccountCodeById;
using Accounting.Application.Common.Interfaces;
using Accounting.Domain.ValueObjects;
using Moq;

namespace Accounting.Application.Tests.AttribForAccountCodes.Queries.GetAttribForAccountCodeById;

public sealed class GetAttribForAccountCodeByIdQueryHandlerTests
{
    private static AttribForAccountCodeDto SampleDto(Guid id) => new(
        Id: id,
        AccountCodeId: Guid.NewGuid(),
        MoinCode: "110101",
        MoinName: "حساب معین تستی",
        AttribBoxNo: 3,
        Flag: AttribFlag.Date,
        LenAtr: 4,
        AttribSum: AttribSum.Summable,
        ControlId: null,
        VahedCode: "0001",
        Year: "1404",
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
        var readRepository = new Mock<IAttribForAccountCodeReadRepository>();
        readRepository
            .Setup(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);

        var handler = new GetAttribForAccountCodeByIdQueryHandler(readRepository.Object);

        var result = await handler.Handle(new GetAttribForAccountCodeByIdQuery(id), CancellationToken.None);

        Assert.Same(expected, result);
    }

    [Fact]
    public async Task Handle_UnknownId_ReturnsNull_NotException()
    {
        var id = Guid.NewGuid();
        var readRepository = new Mock<IAttribForAccountCodeReadRepository>();
        readRepository
            .Setup(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync((AttribForAccountCodeDto?)null);

        var handler = new GetAttribForAccountCodeByIdQueryHandler(readRepository.Object);

        var result = await handler.Handle(new GetAttribForAccountCodeByIdQuery(id), CancellationToken.None);

        Assert.Null(result);
    }

    [Fact]
    public async Task Handle_PropagatesCancellationTokenToRepository()
    {
        var id = Guid.NewGuid();
        var readRepository = new Mock<IAttribForAccountCodeReadRepository>();
        using var cts = new CancellationTokenSource();
        var token = cts.Token;
        readRepository
            .Setup(r => r.GetByIdAsync(id, token))
            .ReturnsAsync((AttribForAccountCodeDto?)null);

        var handler = new GetAttribForAccountCodeByIdQueryHandler(readRepository.Object);

        await handler.Handle(new GetAttribForAccountCodeByIdQuery(id), token);

        readRepository.Verify(r => r.GetByIdAsync(id, token), Times.Once);
    }

    [Fact]
    public void Constructor_DoesNotDependOnIUnitOfWork()
    {
        var parameterTypes = typeof(GetAttribForAccountCodeByIdQueryHandler)
            .GetConstructors()
            .SelectMany(c => c.GetParameters())
            .Select(p => p.ParameterType);

        Assert.DoesNotContain(typeof(IUnitOfWork), parameterTypes);
    }
}
