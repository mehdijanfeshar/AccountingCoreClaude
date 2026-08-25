using Accounting.Api.Controllers;
using Accounting.Application.Common;
using Accounting.Application.Vouchers.Commands.CreateVoucherDetail;
using Accounting.Application.Vouchers.Commands.DeleteVoucherDetail;
using Accounting.Application.Vouchers.Commands.UpdateVoucherDetail;
using Accounting.Application.Vouchers.Queries;
using Accounting.Application.Vouchers.Queries.GetVoucherDetailById;
using Accounting.Application.Vouchers.Queries.GetVoucherDetails;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace Accounting.Api.Tests.Controllers;

/// <summary>
/// Pure HTTP-layer tests for <see cref="VoucherDetailsController"/>. <see cref="IMediator"/> is
/// mocked — no MediatR pipeline, no FluentValidation, no database. These tests exist only to pin
/// down the thin "build request → Send → map result" contract of the controller itself, mirroring
/// <c>VoucherHeadsControllerTests</c> exactly.
/// </summary>
public sealed class VoucherDetailsControllerTests
{
    private static CreateVoucherDetailCommand ValidCreateCommand(Guid voucherHeadId) => new(
        VoucherHeadId: voucherHeadId,
        AccountId: Guid.NewGuid(),
        ReceiptId: null,
        CheckId: null,
        LowLevelCodeId: null,
        EtebarId: null,
        Description: "ردیف اول",
        Radif: 1,
        Debtor: 1000m,
        Creditor: null,
        VahedCode: "0001",
        Year: "1405");

    [Fact]
    public async Task Create_ReturnsCreatedAtActionWithGeneratedIdInBodyAndRouteValues()
    {
        var mediator = new Mock<IMediator>();
        var generatedId = Guid.NewGuid();
        var command = ValidCreateCommand(Guid.NewGuid());

        IRequest<Guid>? capturedRequest = null;
        mediator
            .Setup(m => m.Send(It.IsAny<CreateVoucherDetailCommand>(), It.IsAny<CancellationToken>()))
            .Callback<IRequest<Guid>, CancellationToken>((request, _) => capturedRequest = request)
            .ReturnsAsync(generatedId);

        var controller = new VoucherDetailsController(mediator.Object);

        var actionResult = await controller.Create(command, CancellationToken.None);

        var created = Assert.IsType<CreatedAtActionResult>(actionResult);
        Assert.Equal(nameof(VoucherDetailsController.GetById), created.ActionName);

        var response = Assert.IsType<CreateVoucherDetailResponse>(created.Value);
        Assert.Equal(generatedId, response.Id);

        Assert.NotNull(created.RouteValues);
        Assert.Equal(generatedId, created.RouteValues!["id"]);

        Assert.Same(command, capturedRequest);
    }

    [Fact]
    public async Task Create_ForwardsCancellationTokenToMediator()
    {
        var mediator = new Mock<IMediator>();
        mediator
            .Setup(m => m.Send(It.IsAny<CreateVoucherDetailCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Guid.NewGuid());

        var controller = new VoucherDetailsController(mediator.Object);
        using var cts = new CancellationTokenSource();

        await controller.Create(ValidCreateCommand(Guid.NewGuid()), cts.Token);

        mediator.Verify(
            m => m.Send(It.IsAny<CreateVoucherDetailCommand>(), cts.Token),
            Times.Once);
    }

    [Fact]
    public async Task GetList_PassesPageNumberPageSizeVoucherHeadIdYearAndVahedCodeThroughToQueryUnchanged_AndReturns200WithPagedResult()
    {
        var mediator = new Mock<IMediator>();
        var voucherHeadId = Guid.NewGuid();
        var pagedResult = new PagedResult<VoucherDetailDto>
        {
            Items = Array.Empty<VoucherDetailDto>(),
            PageNumber = 2,
            PageSize = 10,
            TotalCount = 0,
        };

        GetVoucherDetailsQuery? capturedQuery = null;
        mediator
            .Setup(m => m.Send(It.IsAny<GetVoucherDetailsQuery>(), It.IsAny<CancellationToken>()))
            .Callback<IRequest<PagedResult<VoucherDetailDto>>, CancellationToken>(
                (request, _) => capturedQuery = (GetVoucherDetailsQuery)request)
            .ReturnsAsync(pagedResult);

        var controller = new VoucherDetailsController(mediator.Object);

        var actionResult = await controller.GetList(
            pageNumber: 2,
            pageSize: 10,
            voucherHeadId: voucherHeadId,
            year: "1405",
            vahedCode: "0001",
            CancellationToken.None);

        Assert.NotNull(capturedQuery);
        Assert.Equal(2, capturedQuery!.PageNumber);
        Assert.Equal(10, capturedQuery.PageSize);
        Assert.Equal(voucherHeadId, capturedQuery.VoucherHeadId);
        Assert.Equal("1405", capturedQuery.Year);
        Assert.Equal("0001", capturedQuery.VahedCode);

        var ok = Assert.IsType<OkObjectResult>(actionResult);
        Assert.Same(pagedResult, ok.Value);
    }

    [Fact]
    public async Task GetList_DefaultParameterValues_AreAppliedWhenOmitted()
    {
        var mediator = new Mock<IMediator>();
        GetVoucherDetailsQuery? capturedQuery = null;
        mediator
            .Setup(m => m.Send(It.IsAny<GetVoucherDetailsQuery>(), It.IsAny<CancellationToken>()))
            .Callback<IRequest<PagedResult<VoucherDetailDto>>, CancellationToken>(
                (request, _) => capturedQuery = (GetVoucherDetailsQuery)request)
            .ReturnsAsync(new PagedResult<VoucherDetailDto>());

        var controller = new VoucherDetailsController(mediator.Object);

        await controller.GetList();

        Assert.NotNull(capturedQuery);
        Assert.Equal(1, capturedQuery!.PageNumber);
        Assert.Equal(20, capturedQuery.PageSize);
        Assert.Null(capturedQuery.VoucherHeadId);
        Assert.Null(capturedQuery.Year);
        Assert.Null(capturedQuery.VahedCode);
    }

    [Fact]
    public async Task GetList_ForwardsCancellationTokenToMediator()
    {
        var mediator = new Mock<IMediator>();
        mediator
            .Setup(m => m.Send(It.IsAny<GetVoucherDetailsQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PagedResult<VoucherDetailDto>());

        var controller = new VoucherDetailsController(mediator.Object);
        using var cts = new CancellationTokenSource();

        await controller.GetList(1, 20, null, null, null, cts.Token);

        mediator.Verify(
            m => m.Send(It.IsAny<GetVoucherDetailsQuery>(), cts.Token),
            Times.Once);
    }

    private static VoucherDetailDto SampleDto(Guid id) => new(
        Id: id,
        VoucherHeadId: Guid.NewGuid(),
        AccountId: Guid.NewGuid(),
        ReceiptId: null,
        CheckId: null,
        LowLevelCodeId: null,
        EtebarId: null,
        Description: "ردیف",
        Radif: 1,
        Debtor: 1000m,
        Creditor: null,
        CreatedDate: DateTime.UtcNow,
        UpdatedDate: null,
        AddUserId: "user1",
        ChangeUserId: null,
        VahedCode: "0001",
        Year: "1405",
        IsDeleted: false);

    [Fact]
    public async Task GetById_ReturnsOkWithDto_WhenHandlerReturnsValue()
    {
        var mediator = new Mock<IMediator>();
        var id = Guid.NewGuid();
        var dto = SampleDto(id);

        mediator
            .Setup(m => m.Send(It.Is<GetVoucherDetailByIdQuery>(q => q.Id == id), It.IsAny<CancellationToken>()))
            .ReturnsAsync(dto);

        var controller = new VoucherDetailsController(mediator.Object);

        var actionResult = await controller.GetById(id, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(actionResult);
        Assert.Same(dto, ok.Value);
    }

    [Fact]
    public async Task GetById_ReturnsNotFound_WhenHandlerReturnsNull()
    {
        var mediator = new Mock<IMediator>();
        mediator
            .Setup(m => m.Send(It.IsAny<GetVoucherDetailByIdQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((VoucherDetailDto?)null);

        var controller = new VoucherDetailsController(mediator.Object);

        var actionResult = await controller.GetById(Guid.NewGuid(), CancellationToken.None);

        Assert.IsType<NotFoundResult>(actionResult);
    }

    [Fact]
    public async Task GetById_ForwardsCancellationTokenToMediator()
    {
        var mediator = new Mock<IMediator>();
        mediator
            .Setup(m => m.Send(It.IsAny<GetVoucherDetailByIdQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((VoucherDetailDto?)null);

        var controller = new VoucherDetailsController(mediator.Object);
        using var cts = new CancellationTokenSource();
        var id = Guid.NewGuid();

        await controller.GetById(id, cts.Token);

        mediator.Verify(
            m => m.Send(It.Is<GetVoucherDetailByIdQuery>(q => q.Id == id), cts.Token),
            Times.Once);
    }

    private static UpdateVoucherDetailRequest ValidUpdateRequest() => new(
        AccountId: Guid.NewGuid(),
        ReceiptId: null,
        CheckId: null,
        LowLevelCodeId: null,
        EtebarId: null,
        Description: "ردیف اصلاحی",
        Radif: 2,
        Debtor: null,
        Creditor: 2500m,
        VahedCode: "0002",
        Year: "1404");

    [Fact]
    public async Task Update_ReturnsOkWithId_AndSendsCommandThroughMediator()
    {
        var mediator = new Mock<IMediator>();
        var id = Guid.NewGuid();
        mediator
            .Setup(m => m.Send(It.IsAny<UpdateVoucherDetailCommand>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var controller = new VoucherDetailsController(mediator.Object);

        var actionResult = await controller.Update(id, ValidUpdateRequest(), CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(actionResult);
        var response = Assert.IsType<UpdateVoucherDetailResponse>(ok.Value);
        Assert.Equal(id, response.Id);

        mediator.Verify(
            m => m.Send(It.Is<UpdateVoucherDetailCommand>(c => c.Id == id), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    /// <summary>
    /// The single most important security assertion for this controller's Update action: the
    /// command sent to the handler must always carry the route id, never any id embedded in the
    /// request body (the body's DTO does not even have an Id property).
    /// </summary>
    [Fact]
    public async Task Update_BuildsCommandFromRouteId_NotFromAnyBodyValue()
    {
        var mediator = new Mock<IMediator>();
        var routeId = Guid.NewGuid();
        UpdateVoucherDetailCommand? capturedCommand = null;
        mediator
            .Setup(m => m.Send(It.IsAny<UpdateVoucherDetailCommand>(), It.IsAny<CancellationToken>()))
            .Callback<IRequest, CancellationToken>((request, _) => capturedCommand = (UpdateVoucherDetailCommand)request)
            .Returns(Task.CompletedTask);

        var controller = new VoucherDetailsController(mediator.Object);

        await controller.Update(routeId, ValidUpdateRequest(), CancellationToken.None);

        Assert.NotNull(capturedCommand);
        Assert.Equal(routeId, capturedCommand!.Id);
    }

    [Fact]
    public async Task Update_ForwardsCancellationTokenToMediator()
    {
        var mediator = new Mock<IMediator>();
        mediator
            .Setup(m => m.Send(It.IsAny<UpdateVoucherDetailCommand>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var controller = new VoucherDetailsController(mediator.Object);
        using var cts = new CancellationTokenSource();

        await controller.Update(Guid.NewGuid(), ValidUpdateRequest(), cts.Token);

        mediator.Verify(
            m => m.Send(It.IsAny<UpdateVoucherDetailCommand>(), cts.Token),
            Times.Once);
    }

    [Fact]
    public async Task Delete_ReturnsOkWithId_AndSendsCommandWithRouteId()
    {
        var mediator = new Mock<IMediator>();
        var id = Guid.NewGuid();
        mediator
            .Setup(m => m.Send(It.IsAny<DeleteVoucherDetailCommand>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var controller = new VoucherDetailsController(mediator.Object);

        var actionResult = await controller.Delete(id, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(actionResult);
        var response = Assert.IsType<DeleteVoucherDetailResponse>(ok.Value);
        Assert.Equal(id, response.Id);

        mediator.Verify(
            m => m.Send(It.Is<DeleteVoucherDetailCommand>(c => c.Id == id), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Delete_ForwardsCancellationTokenToMediator()
    {
        var mediator = new Mock<IMediator>();
        mediator
            .Setup(m => m.Send(It.IsAny<DeleteVoucherDetailCommand>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var controller = new VoucherDetailsController(mediator.Object);
        using var cts = new CancellationTokenSource();

        await controller.Delete(Guid.NewGuid(), cts.Token);

        mediator.Verify(
            m => m.Send(It.IsAny<DeleteVoucherDetailCommand>(), cts.Token),
            Times.Once);
    }
}
