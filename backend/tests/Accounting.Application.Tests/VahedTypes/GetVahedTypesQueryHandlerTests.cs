using Accounting.Application.Common.Interfaces;
using Accounting.Application.VahedTypes.Queries;
using Accounting.Application.VahedTypes.Queries.GetVahedTypes;
using Moq;

namespace Accounting.Application.Tests.VahedTypes;

public sealed class GetVahedTypesQueryHandlerTests
{
    [Fact]
    public async Task Handle_ReturnsRepositoryResult()
    {
        var expected = new List<VahedTypeDto>
        {
            new(Guid.NewGuid(), "1", "اداره کل", "1"),
            new(Guid.NewGuid(), "3", "بيمارستان", "2"),
        };
        var readRepository = new Mock<IVahedTypeReadRepository>();
        readRepository
            .Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);

        var handler = new GetVahedTypesQueryHandler(readRepository.Object);

        var result = await handler.Handle(new GetVahedTypesQuery(), CancellationToken.None);

        Assert.Same(expected, result);
    }

    [Fact]
    public async Task Handle_PropagatesCancellationTokenToRepository()
    {
        using var cts = new CancellationTokenSource();
        var token = cts.Token;
        var readRepository = new Mock<IVahedTypeReadRepository>();
        readRepository
            .Setup(r => r.GetAllAsync(token))
            .ReturnsAsync([]);

        var handler = new GetVahedTypesQueryHandler(readRepository.Object);

        await handler.Handle(new GetVahedTypesQuery(), token);

        readRepository.Verify(r => r.GetAllAsync(token), Times.Once);
    }

    [Fact]
    public void Constructor_DoesNotDependOnIUnitOfWork()
    {
        var parameterTypes = typeof(GetVahedTypesQueryHandler)
            .GetConstructors()
            .SelectMany(c => c.GetParameters())
            .Select(p => p.ParameterType);

        Assert.DoesNotContain(typeof(IUnitOfWork), parameterTypes);
    }

    /// <summary>
    /// The query must stay parameterless. It is a global lookup with no unit-scoped subject, and
    /// adding a parameter would be the moment someone had to decide whether it needs scoping —
    /// this test makes that moment visible instead of silent.
    /// </summary>
    [Fact]
    public void Query_HasNoParameters()
    {
        var constructors = typeof(GetVahedTypesQuery).GetConstructors();

        Assert.All(constructors, c => Assert.Empty(c.GetParameters()));
    }
}
