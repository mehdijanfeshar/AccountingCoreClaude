using Accounting.Api.Controllers;
using Accounting.Application.Accounts.Commands.CreateAccountCode;
using Accounting.Application.Accounts.Queries;
using Accounting.Application.Accounts.Queries.GetAccountCodeById;
using Accounting.Application.Accounts.Queries.GetAccountCodes;
using Accounting.Application.Common;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace Accounting.Api.Tests.Controllers;

/// <summary>
/// Pure HTTP-layer tests for <see cref="AccountCodesController"/>. <see cref="IMediator"/> is
/// mocked — no MediatR pipeline, no FluentValidation, no database. These tests exist only to
/// pin down the thin "build request → Send → map result" contract of the controller itself.
/// </summary>
public sealed class AccountCodesControllerTests
{
    private static CreateAccountCodeCommand ValidCommand() => new(
        TypeCode: true,
        ParentId: null,
        AccCode: "100100",
        AccCodeName: "بانک ملی",
        TypeActivity: true,
        SourceAndConsumeId: null,
        IdentyGroupsId: null,
        TypeAccCode: true,
        AddUserId: "user1",
        MoInforClose: null,
        TypeAction: null);

    [Fact]
    public async Task Create_ReturnsCreatedAtActionWithGeneratedIdInBodyAndRouteValues()
    {
        var mediator = new Mock<IMediator>();
        var generatedId = Guid.NewGuid();
        var command = ValidCommand();

        IRequest<Guid>? capturedRequest = null;
        mediator
            .Setup(m => m.Send(It.IsAny<CreateAccountCodeCommand>(), It.IsAny<CancellationToken>()))
            .Callback<IRequest<Guid>, CancellationToken>((request, _) => capturedRequest = request)
            .ReturnsAsync(generatedId);

        var controller = new AccountCodesController(mediator.Object);

        var actionResult = await controller.Create(command, CancellationToken.None);

        var created = Assert.IsType<CreatedAtActionResult>(actionResult);
        Assert.Equal(nameof(AccountCodesController.GetById), created.ActionName);

        var response = Assert.IsType<CreateAccountCodeResponse>(created.Value);
        Assert.Equal(generatedId, response.Id);

        Assert.NotNull(created.RouteValues);
        Assert.Equal(generatedId, created.RouteValues!["id"]);

        // The exact command instance handed to the controller must be the exact instance
        // forwarded to IMediator.Send — no rebuilding/copying along the way.
        Assert.Same(command, capturedRequest);
    }

    [Fact]
    public async Task Create_ForwardsCancellationTokenToMediator()
    {
        var mediator = new Mock<IMediator>();
        mediator
            .Setup(m => m.Send(It.IsAny<CreateAccountCodeCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Guid.NewGuid());

        var controller = new AccountCodesController(mediator.Object);
        using var cts = new CancellationTokenSource();

        await controller.Create(ValidCommand(), cts.Token);

        mediator.Verify(
            m => m.Send(It.IsAny<CreateAccountCodeCommand>(), cts.Token),
            Times.Once);
    }

    [Fact]
    public async Task GetList_PassesPageNumberAndPageSizeThroughToQueryUnchanged_AndReturns200WithPagedResult()
    {
        var mediator = new Mock<IMediator>();
        var pagedResult = new PagedResult<AccountCodeDto>
        {
            Items = Array.Empty<AccountCodeDto>(),
            PageNumber = 3,
            PageSize = 15,
            TotalCount = 0,
        };

        GetAccountCodesQuery? capturedQuery = null;
        mediator
            .Setup(m => m.Send(It.IsAny<GetAccountCodesQuery>(), It.IsAny<CancellationToken>()))
            .Callback<IRequest<PagedResult<AccountCodeDto>>, CancellationToken>(
                (request, _) => capturedQuery = (GetAccountCodesQuery)request)
            .ReturnsAsync(pagedResult);

        var controller = new AccountCodesController(mediator.Object);

        var actionResult = await controller.GetList(pageNumber: 3, pageSize: 15, CancellationToken.None);

        Assert.NotNull(capturedQuery);
        Assert.Equal(3, capturedQuery!.PageNumber);
        Assert.Equal(15, capturedQuery.PageSize);

        var ok = Assert.IsType<OkObjectResult>(actionResult);
        Assert.Same(pagedResult, ok.Value);
    }

    [Fact]
    public async Task GetList_DefaultParameterValues_AreAppliedWhenOmitted()
    {
        var mediator = new Mock<IMediator>();
        GetAccountCodesQuery? capturedQuery = null;
        mediator
            .Setup(m => m.Send(It.IsAny<GetAccountCodesQuery>(), It.IsAny<CancellationToken>()))
            .Callback<IRequest<PagedResult<AccountCodeDto>>, CancellationToken>(
                (request, _) => capturedQuery = (GetAccountCodesQuery)request)
            .ReturnsAsync(new PagedResult<AccountCodeDto>());

        var controller = new AccountCodesController(mediator.Object);

        // Simulates the query string omitting both pageNumber and pageSize — the C# compiler
        // substitutes the action's own default parameter values, exactly as ASP.NET Core model
        // binding would when the query string does not contain those keys.
        await controller.GetList();

        Assert.NotNull(capturedQuery);
        Assert.Equal(1, capturedQuery!.PageNumber);
        Assert.Equal(20, capturedQuery.PageSize);
    }

    [Fact]
    public async Task GetList_ForwardsCancellationTokenToMediator()
    {
        var mediator = new Mock<IMediator>();
        mediator
            .Setup(m => m.Send(It.IsAny<GetAccountCodesQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PagedResult<AccountCodeDto>());

        var controller = new AccountCodesController(mediator.Object);
        using var cts = new CancellationTokenSource();

        await controller.GetList(1, 20, cts.Token);

        mediator.Verify(
            m => m.Send(It.IsAny<GetAccountCodesQuery>(), cts.Token),
            Times.Once);
    }

    [Fact]
    public async Task GetById_ReturnsOkWithDto_WhenHandlerReturnsValue()
    {
        var mediator = new Mock<IMediator>();
        var id = Guid.NewGuid();
        var dto = new AccountCodeDto(
            Id: id,
            TypeCode: true,
            ParentId: null,
            AccCode: "100100",
            AccCodeName: "بانک ملی",
            TypeActivity: true,
            SourceAndConsumeId: null,
            IdentyGroupsId: null,
            TypeAccCode: true,
            CreatedDate: DateTime.UtcNow,
            UpdatedDate: null,
            AddUserId: "user1",
            ChangeUserId: null,
            IsDeleted: false,
            MoInforClose: null,
            TypeAction: null);

        mediator
            .Setup(m => m.Send(It.Is<GetAccountCodeByIdQuery>(q => q.Id == id), It.IsAny<CancellationToken>()))
            .ReturnsAsync(dto);

        var controller = new AccountCodesController(mediator.Object);

        var actionResult = await controller.GetById(id, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(actionResult);
        Assert.Same(dto, ok.Value);
    }

    /// <summary>
    /// The single most important behavioral assertion for this controller: a missing row must
    /// surface as a bodyless 404, not an empty 200 or an unhandled null-reference.
    /// </summary>
    [Fact]
    public async Task GetById_ReturnsNotFound_WhenHandlerReturnsNull()
    {
        var mediator = new Mock<IMediator>();
        mediator
            .Setup(m => m.Send(It.IsAny<GetAccountCodeByIdQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((AccountCodeDto?)null);

        var controller = new AccountCodesController(mediator.Object);

        var actionResult = await controller.GetById(Guid.NewGuid(), CancellationToken.None);

        Assert.IsType<NotFoundResult>(actionResult);
    }

    [Fact]
    public async Task GetById_ForwardsCancellationTokenToMediator()
    {
        var mediator = new Mock<IMediator>();
        mediator
            .Setup(m => m.Send(It.IsAny<GetAccountCodeByIdQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((AccountCodeDto?)null);

        var controller = new AccountCodesController(mediator.Object);
        using var cts = new CancellationTokenSource();
        var id = Guid.NewGuid();

        await controller.GetById(id, cts.Token);

        mediator.Verify(
            m => m.Send(It.Is<GetAccountCodeByIdQuery>(q => q.Id == id), cts.Token),
            Times.Once);
    }
}
