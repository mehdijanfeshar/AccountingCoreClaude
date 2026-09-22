using Accounting.Application.Tafsilis.Queries;
using Accounting.Application.Tafsilis.Queries.GetTafsiliById;
using Accounting.Application.Common.Interfaces;
using Accounting.Domain.ValueObjects;
using Moq;

namespace Accounting.Application.Tests.Tafsilis.Queries.GetTafsiliById;

public sealed class GetTafsiliByIdQueryHandlerTests
{
    private static TafsiliDto SampleDto(Guid id) => new(
        Id: id,
        TafsiliCode: "001",
        TafsiliName: "تفصیلی یک",
        TafsilDesc: null,
        IsActive: TafsiliActiveState.IsActive,
        PersonType: null,
        Owner: null,
        VahedType: null,
        VahedCode: "1001",
        TafsilGroupIds: Array.Empty<Guid>(),
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
        var readRepository = new Mock<ITafsiliReadRepository>();
        readRepository
            .Setup(r => r.GetByIdAsync(id, It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);

        var handler = new GetTafsiliByIdQueryHandler(readRepository.Object);

        var result = await handler.Handle(new GetTafsiliByIdQuery(id), CancellationToken.None);

        Assert.Same(expected, result);
    }

    [Fact]
    public async Task Handle_UnknownId_ReturnsNull_NotException()
    {
        var id = Guid.NewGuid();
        var readRepository = new Mock<ITafsiliReadRepository>();
        readRepository
            .Setup(r => r.GetByIdAsync(id, It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((TafsiliDto?)null);

        var handler = new GetTafsiliByIdQueryHandler(readRepository.Object);

        var result = await handler.Handle(new GetTafsiliByIdQuery(id), CancellationToken.None);

        Assert.Null(result);
    }

    [Fact]
    public void Constructor_DoesNotDependOnIUnitOfWork()
    {
        var parameterTypes = typeof(GetTafsiliByIdQueryHandler)
            .GetConstructors()
            .SelectMany(c => c.GetParameters())
            .Select(p => p.ParameterType);

        Assert.DoesNotContain(typeof(IUnitOfWork), parameterTypes);
    }
}
