using Accounting.Application.Common.Exceptions;
using Accounting.Application.Common.Interfaces;
using Accounting.Application.Tests.TestSupport;
using Accounting.Application.Vouchers.Commands.DeleteVoucherHead;
using Accounting.Application.Vouchers.Commands.UpdateVoucherHead;
using Accounting.Domain.Entity;
using Accounting.Domain.ValueObjects;
using Moq;

namespace Accounting.Application.Tests.Vouchers.Commands.Common;

/// <summary>
/// Proves the editability rule is actually reached by the voucher write paths, not merely
/// implemented. Same shape and motivation as <c>VoucherHandlersEnforceTafsiliLevelsTests</c>: a
/// rule that exists but is never called is the exact failure phase 31 and phase 35 each had to
/// undo.
/// </summary>
public sealed class VoucherHandlersEnforceEditabilityTests
{
    private static Mock<ICurrentUser> CurrentUser()
    {
        var currentUser = new Mock<ICurrentUser>();
        currentUser.SetupGet(u => u.UserId).Returns("tester1");
        currentUser.SetupGet(u => u.VahedCode).Returns("0001");
        return currentUser;
    }

    private static Mock<IVoucherHeadRepository> HeadRepositoryReturning(Guid id, DocLife? docLife, bool? isDeleted = false)
    {
        var repository = new Mock<IVoucherHeadRepository>();

        repository
            .Setup(r => r.GetForUpdateAsync(id, It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new TB_VOUCHERSHEAD
            {
                ID = id,
                DOCLIFE = docLife,
                ISDELETED = isDeleted,
                DOC_NUM = "000001",
                DATE_DOC = "14030101",
                VAHEDCODE = "0001",
            });

        return repository;
    }

    [Theory]
    [InlineData(DocLife.Reviewed)]
    [InlineData(DocLife.Accepted)]
    public async Task DeleteVoucherHead_refuses_a_locked_voucher(DocLife docLife)
    {
        var id = Guid.NewGuid();
        var repository = HeadRepositoryReturning(id, docLife);
        var unitOfWork = new Mock<IUnitOfWork>();

        var handler = new DeleteVoucherHeadCommandHandler(repository.Object, unitOfWork.Object, CurrentUser().Object);

        await Assert.ThrowsAsync<VoucherNotEditableException>(
            () => handler.Handle(new DeleteVoucherHeadCommand(id), CancellationToken.None));

        // Nothing may be staged or persisted on refusal.
        repository.Verify(
            r => r.SoftDeleteDetailTreeAsync(
                It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()),
            Times.Never);
        unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Theory]
    [InlineData(DocLife.Reviewed)]
    [InlineData(DocLife.Accepted)]
    public async Task UpdateVoucherHead_refuses_a_locked_voucher(DocLife docLife)
    {
        var id = Guid.NewGuid();
        var repository = HeadRepositoryReturning(id, docLife);
        var unitOfWork = new Mock<IUnitOfWork>();

        var handler = new UpdateVoucherHeadCommandHandler(repository.Object, unitOfWork.Object, CurrentUser().Object);

        await Assert.ThrowsAsync<VoucherNotEditableException>(
            () => handler.Handle(UpdateCommand(id), CancellationToken.None));

        unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Theory]
    [InlineData(DocLife.Draft)]
    [InlineData(DocLife.Temporary)]
    public async Task DeleteVoucherHead_allows_an_editable_voucher(DocLife docLife)
    {
        var id = Guid.NewGuid();
        var repository = HeadRepositoryReturning(id, docLife);
        var unitOfWork = new Mock<IUnitOfWork>();

        var handler = new DeleteVoucherHeadCommandHandler(repository.Object, unitOfWork.Object, CurrentUser().Object);

        await handler.Handle(new DeleteVoucherHeadCommand(id), CancellationToken.None);

        unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    /// <summary>
    /// The guard runs before the already-deleted short-circuit, so a locked voucher explains
    /// itself rather than reporting a silent success.
    /// </summary>
    [Fact]
    public async Task DeleteVoucherHead_reports_the_lock_even_when_already_soft_deleted()
    {
        var id = Guid.NewGuid();
        var repository = HeadRepositoryReturning(id, DocLife.Accepted, isDeleted: true);
        var unitOfWork = new Mock<IUnitOfWork>();

        var handler = new DeleteVoucherHeadCommandHandler(repository.Object, unitOfWork.Object, CurrentUser().Object);

        await Assert.ThrowsAsync<VoucherNotEditableException>(
            () => handler.Handle(new DeleteVoucherHeadCommand(id), CancellationToken.None));
    }

    private static UpdateVoucherHeadCommand UpdateCommand(Guid id) => new(
        id,
        DocNum: "000002",
        DateDoc: "14030102",
        DocLife: DocLife.Draft,
        HeadDesc: "شرح",
        Apendix: null,
        SystemTypeId: null,
        FlagState: null,
        Year: "1403",
        IsAutomatic: null,
        SndVahedCode: null,
        ParentHeadId: null,
        AttachFileName: null,
        AtfNum: null);
}
