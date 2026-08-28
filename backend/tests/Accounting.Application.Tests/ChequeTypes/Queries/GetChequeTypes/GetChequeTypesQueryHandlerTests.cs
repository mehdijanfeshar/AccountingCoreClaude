using Accounting.Application.ChequeTypes.Queries;
using Accounting.Application.ChequeTypes.Queries.GetChequeTypes;
using Accounting.Application.Common;
using Accounting.Application.Common.Interfaces;
using Moq;

namespace Accounting.Application.Tests.ChequeTypes.Queries.GetChequeTypes;

public sealed class GetChequeTypesQueryHandlerTests
{
    private static ChequeTypeDto SampleDto(Guid id) => new(
        Id: id,
        ChequeTypeTitle: "Standard",
        ChequeWidth: 200,
        ChequeHeight: 90,
        ChequeImage: null,
        ChequeAdateFont: null,
        ChequeAdateLeft: null,
        ChequeAdateTop: null,
        ChequeAdateWidth: null,
        ChequeNdateFont: null,
        ChequeNdateLeft: null,
        ChequeNdateTop: null,
        ChequeNdateWidth: null,
        ChequeAamountFont: null,
        ChequeAamountLeft: null,
        ChequeAamountTop: null,
        ChequeAamountWidth: null,
        ChequeLamountFont: null,
        ChequeLamountLeft: null,
        ChequeLamountTop: null,
        ChequeLamountWidth: null,
        ChequeNamountFont: null,
        ChequeNamountLeft: null,
        ChequeNamountTop: null,
        ChequeNamountWidth: null,
        ChequeDescribe1Font: null,
        ChequeDescribe1Left: null,
        ChequeDescribe1Top: null,
        ChequeDescribe1Width: null,
        ChequeDescribe2Font: null,
        ChequeDescribe2Left: null,
        ChequeDescribe2Top: null,
        ChequeDescribe2Width: null,
        ChequeBreaklineFont: null,
        ChequeBreaklineLeft: null,
        ChequeBreaklineTop: null,
        ChequeBreaklineWidth: null,
        PrinterMargineTop: null,
        PrinterMargineLeft: null,
        PrinterType: null,
        Year: "1403",
        VahedCode: "0100",
        CreatedDate: DateTime.UtcNow,
        UpdatedDate: null,
        AddUserId: "user1",
        ChangeUserId: null,
        IsDeleted: false);

    [Fact]
    public async Task Handle_PassesPageNumberAndPageSizeToRepository_AndReturnsRepositoryResult()
    {
        var readRepository = new Mock<IChequeTypeReadRepository>();
        var expected = new PagedResult<ChequeTypeDto>
        {
            Items = new[] { SampleDto(Guid.NewGuid()) },
            PageNumber = 2,
            PageSize = 25,
            TotalCount = 51,
        };
        readRepository
            .Setup(r => r.GetPagedAsync(2, 25, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);

        var handler = new GetChequeTypesQueryHandler(readRepository.Object);

        var result = await handler.Handle(new GetChequeTypesQuery(PageNumber: 2, PageSize: 25), CancellationToken.None);

        Assert.Same(expected, result);
        Assert.Equal(2, result.PageNumber);
        Assert.Equal(25, result.PageSize);
        Assert.Equal(51, result.TotalCount);
        readRepository.Verify(r => r.GetPagedAsync(2, 25, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_PropagatesCancellationTokenToRepository()
    {
        var readRepository = new Mock<IChequeTypeReadRepository>();
        using var cts = new CancellationTokenSource();
        var token = cts.Token;
        readRepository
            .Setup(r => r.GetPagedAsync(1, 20, token))
            .ReturnsAsync(new PagedResult<ChequeTypeDto>());

        var handler = new GetChequeTypesQueryHandler(readRepository.Object);

        await handler.Handle(new GetChequeTypesQuery(PageNumber: 1, PageSize: 20), token);

        readRepository.Verify(r => r.GetPagedAsync(1, 20, token), Times.Once);
    }

    [Fact]
    public void Constructor_DoesNotDependOnIUnitOfWork()
    {
        var parameterTypes = typeof(GetChequeTypesQueryHandler)
            .GetConstructors()
            .SelectMany(c => c.GetParameters())
            .Select(p => p.ParameterType);

        Assert.DoesNotContain(typeof(IUnitOfWork), parameterTypes);
    }
}
