using Accounting.Api.Controllers;
using Accounting.Application.Common;
using Accounting.Application.Vouchers.Commands.CreateVoucherHead;
using Accounting.Application.Vouchers.Commands.DeleteVoucherHead;
using Accounting.Application.Vouchers.Commands.UpdateVoucherHead;
using Accounting.Application.Vouchers.Queries;
using Accounting.Application.Vouchers.Queries.GetVoucherHeadById;
using Accounting.Application.Vouchers.Queries.GetVoucherHeads;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace Accounting.Api.Tests.Controllers;

/// <summary>
/// Pure HTTP-layer tests for <see cref="VoucherHeadsController"/>. <see cref="IMediator"/> is
/// mocked — no MediatR pipeline, no FluentValidation, no database. These tests exist only to
/// pin down the thin "build request → Send → map result" contract of the controller itself.
/// </summary>
public sealed class VoucherHeadsControllerTests
{
    private static CreateVoucherHeadCommand ValidCommand() => new(
        DocNum: "000001",
        DateDoc: "14030101",
        DocLife: null,
        HeadDesc: "سند افتتاحیه",
        Apendix: null,
        SystemTypeId: null,
        FlagState: null,
        VahedCode: "0001",
        Year: "1403",
        IsAutomatic: false,
        SndVahedCode: null,
        ParentHeadId: null,
        AttachFileName: null,
        AtfNum: null);

    [Fact]
    public async Task Create_ReturnsCreatedAtActionWithGeneratedIdInBodyAndRouteValues()
    {
        var mediator = new Mock<IMediator>();
        var generatedId = Guid.NewGuid();
        var command = ValidCommand();

        IRequest<Guid>? capturedRequest = null;
        mediator
            .Setup(m => m.Send(It.IsAny<CreateVoucherHeadCommand>(), It.IsAny<CancellationToken>()))
            .Callback<IRequest<Guid>, CancellationToken>((request, _) => capturedRequest = request)
            .ReturnsAsync(generatedId);

        var controller = new VoucherHeadsController(mediator.Object);

        var actionResult = await controller.Create(command, CancellationToken.None);

        var created = Assert.IsType<CreatedAtActionResult>(actionResult);
        Assert.Equal(nameof(VoucherHeadsController.GetById), created.ActionName);

        var response = Assert.IsType<CreateVoucherHeadResponse>(created.Value);
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
            .Setup(m => m.Send(It.IsAny<CreateVoucherHeadCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Guid.NewGuid());

        var controller = new VoucherHeadsController(mediator.Object);
        using var cts = new CancellationTokenSource();

        await controller.Create(ValidCommand(), cts.Token);

        mediator.Verify(
            m => m.Send(It.IsAny<CreateVoucherHeadCommand>(), cts.Token),
            Times.Once);
    }

    [Fact]
    public async Task GetList_PassesPageNumberPageSizeYearAndVahedCodeThroughToQueryUnchanged_AndReturns200WithPagedResult()
    {
        var mediator = new Mock<IMediator>();
        var pagedResult = new PagedResult<VoucherHeadDto>
        {
            Items = Array.Empty<VoucherHeadDto>(),
            PageNumber = 2,
            PageSize = 10,
            TotalCount = 0,
        };

        GetVoucherHeadsQuery? capturedQuery = null;
        mediator
            .Setup(m => m.Send(It.IsAny<GetVoucherHeadsQuery>(), It.IsAny<CancellationToken>()))
            .Callback<IRequest<PagedResult<VoucherHeadDto>>, CancellationToken>(
                (request, _) => capturedQuery = (GetVoucherHeadsQuery)request)
            .ReturnsAsync(pagedResult);

        var controller = new VoucherHeadsController(mediator.Object);

        var actionResult = await controller.GetList(
            pageNumber: 2,
            pageSize: 10,
            year: "1403",
            vahedCode: "0001",
            CancellationToken.None);

        Assert.NotNull(capturedQuery);
        Assert.Equal(2, capturedQuery!.PageNumber);
        Assert.Equal(10, capturedQuery.PageSize);
        Assert.Equal("1403", capturedQuery.Year);
        Assert.Equal("0001", capturedQuery.VahedCode);

        var ok = Assert.IsType<OkObjectResult>(actionResult);
        Assert.Same(pagedResult, ok.Value);
    }

    [Fact]
    public async Task GetList_DefaultParameterValues_AreAppliedWhenOmitted()
    {
        var mediator = new Mock<IMediator>();
        GetVoucherHeadsQuery? capturedQuery = null;
        mediator
            .Setup(m => m.Send(It.IsAny<GetVoucherHeadsQuery>(), It.IsAny<CancellationToken>()))
            .Callback<IRequest<PagedResult<VoucherHeadDto>>, CancellationToken>(
                (request, _) => capturedQuery = (GetVoucherHeadsQuery)request)
            .ReturnsAsync(new PagedResult<VoucherHeadDto>());

        var controller = new VoucherHeadsController(mediator.Object);

        // Simulates the query string omitting pageNumber/pageSize/year/vahedCode — the C#
        // compiler substitutes the action's own default parameter values, exactly as ASP.NET
        // Core model binding would when the query string does not contain those keys.
        await controller.GetList();

        Assert.NotNull(capturedQuery);
        Assert.Equal(1, capturedQuery!.PageNumber);
        Assert.Equal(20, capturedQuery.PageSize);
        Assert.Null(capturedQuery.Year);
        Assert.Null(capturedQuery.VahedCode);
    }

    [Fact]
    public async Task GetList_ForwardsCancellationTokenToMediator()
    {
        var mediator = new Mock<IMediator>();
        mediator
            .Setup(m => m.Send(It.IsAny<GetVoucherHeadsQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PagedResult<VoucherHeadDto>());

        var controller = new VoucherHeadsController(mediator.Object);
        using var cts = new CancellationTokenSource();

        await controller.GetList(1, 20, null, null, cts.Token);

        mediator.Verify(
            m => m.Send(It.IsAny<GetVoucherHeadsQuery>(), cts.Token),
            Times.Once);
    }

    [Fact]
    public async Task GetById_ReturnsOkWithDto_WhenHandlerReturnsValue()
    {
        var mediator = new Mock<IMediator>();
        var id = Guid.NewGuid();
        var dto = new VoucherHeadDto(
            Id: id,
            DocNum: "000001",
            DateDoc: "14030101",
            DocLife: null,
            HeadDesc: "سند افتتاحیه",
            Apendix: null,
            SystemTypeId: null,
            FlagState: null,
            CreatedDate: DateTime.UtcNow,
            UpdatedDate: null,
            AddUserId: "user1",
            ChangeUserId: null,
            VahedCode: "0001",
            Year: "1403",
            IsDeleted: false,
            AttachFileName: null,
            AtfNum: null,
            IsAutomatic: false,
            SndVahedCode: null,
            ParentHeadId: null,
            GlobalNumber: null);

        mediator
            .Setup(m => m.Send(It.Is<GetVoucherHeadByIdQuery>(q => q.Id == id), It.IsAny<CancellationToken>()))
            .ReturnsAsync(dto);

        var controller = new VoucherHeadsController(mediator.Object);

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
            .Setup(m => m.Send(It.IsAny<GetVoucherHeadByIdQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((VoucherHeadDto?)null);

        var controller = new VoucherHeadsController(mediator.Object);

        var actionResult = await controller.GetById(Guid.NewGuid(), CancellationToken.None);

        Assert.IsType<NotFoundResult>(actionResult);
    }

    [Fact]
    public async Task GetById_ForwardsCancellationTokenToMediator()
    {
        var mediator = new Mock<IMediator>();
        mediator
            .Setup(m => m.Send(It.IsAny<GetVoucherHeadByIdQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((VoucherHeadDto?)null);

        var controller = new VoucherHeadsController(mediator.Object);
        using var cts = new CancellationTokenSource();
        var id = Guid.NewGuid();

        await controller.GetById(id, cts.Token);

        mediator.Verify(
            m => m.Send(It.Is<GetVoucherHeadByIdQuery>(q => q.Id == id), cts.Token),
            Times.Once);
    }

    private static UpdateVoucherHeadRequest ValidUpdateRequest() => new(
        DocNum: "000002",
        DateDoc: "14030202",
        DocLife: true,
        HeadDesc: "سند اصلاحی",
        Apendix: null,
        SystemTypeId: null,
        FlagState: null,
        VahedCode: "0002",
        Year: "1404",
        IsAutomatic: true,
        SndVahedCode: null,
        ParentHeadId: null,
        AttachFileName: null,
        AtfNum: null);

    [Fact]
    public async Task Update_ReturnsOkWithId_AndSendsCommandThroughMediator()
    {
        var mediator = new Mock<IMediator>();
        var id = Guid.NewGuid();
        mediator
            .Setup(m => m.Send(It.IsAny<UpdateVoucherHeadCommand>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var controller = new VoucherHeadsController(mediator.Object);

        var actionResult = await controller.Update(id, ValidUpdateRequest(), CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(actionResult);
        var response = Assert.IsType<UpdateVoucherHeadResponse>(ok.Value);
        Assert.Equal(id, response.Id);

        mediator.Verify(
            m => m.Send(It.Is<UpdateVoucherHeadCommand>(c => c.Id == id), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    /// <summary>
    /// The single most important security assertion for this controller's Update action: the
    /// command sent to the handler must always carry the route id, never any id embedded in the
    /// request body (the body's DTO does not even have an Id property — this test proves the
    /// route value is what actually reaches MediatR).
    /// </summary>
    [Fact]
    public async Task Update_BuildsCommandFromRouteId_NotFromAnyBodyValue()
    {
        var mediator = new Mock<IMediator>();
        var routeId = Guid.NewGuid();
        UpdateVoucherHeadCommand? capturedCommand = null;
        mediator
            .Setup(m => m.Send(It.IsAny<UpdateVoucherHeadCommand>(), It.IsAny<CancellationToken>()))
            .Callback<IRequest, CancellationToken>((request, _) => capturedCommand = (UpdateVoucherHeadCommand)request)
            .Returns(Task.CompletedTask);

        var controller = new VoucherHeadsController(mediator.Object);

        await controller.Update(routeId, ValidUpdateRequest(), CancellationToken.None);

        Assert.NotNull(capturedCommand);
        Assert.Equal(routeId, capturedCommand!.Id);
    }

    [Fact]
    public async Task Update_ForwardsCancellationTokenToMediator()
    {
        var mediator = new Mock<IMediator>();
        mediator
            .Setup(m => m.Send(It.IsAny<UpdateVoucherHeadCommand>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var controller = new VoucherHeadsController(mediator.Object);
        using var cts = new CancellationTokenSource();

        await controller.Update(Guid.NewGuid(), ValidUpdateRequest(), cts.Token);

        mediator.Verify(
            m => m.Send(It.IsAny<UpdateVoucherHeadCommand>(), cts.Token),
            Times.Once);
    }

    [Fact]
    public async Task Delete_ReturnsOkWithId_AndSendsCommandWithRouteId()
    {
        var mediator = new Mock<IMediator>();
        var id = Guid.NewGuid();
        mediator
            .Setup(m => m.Send(It.IsAny<DeleteVoucherHeadCommand>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var controller = new VoucherHeadsController(mediator.Object);

        var actionResult = await controller.Delete(id, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(actionResult);
        var response = Assert.IsType<DeleteVoucherHeadResponse>(ok.Value);
        Assert.Equal(id, response.Id);

        mediator.Verify(
            m => m.Send(It.Is<DeleteVoucherHeadCommand>(c => c.Id == id), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Delete_ForwardsCancellationTokenToMediator()
    {
        var mediator = new Mock<IMediator>();
        mediator
            .Setup(m => m.Send(It.IsAny<DeleteVoucherHeadCommand>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var controller = new VoucherHeadsController(mediator.Object);
        using var cts = new CancellationTokenSource();

        await controller.Delete(Guid.NewGuid(), cts.Token);

        mediator.Verify(
            m => m.Send(It.IsAny<DeleteVoucherHeadCommand>(), cts.Token),
            Times.Once);
    }
}
