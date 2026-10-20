using Accounting.Application.Commands.Vouchers.ChangeVoucherState;
using Accounting.Application.Commands.Vouchers.CreateVoucherDetail;
using Accounting.Application.Commands.Vouchers.CreateVoucherHead;
using Accounting.Application.Commands.Vouchers.DeleteVoucherDetail;
using Accounting.Application.Commands.Vouchers.SaveVoucher;
using Accounting.Application.Commands.Vouchers.UpdateVoucherDetail;
using Accounting.Application.Commands.Vouchers.UpdateVoucherHead;
using Accounting.Application.Common.Exceptions;
using Accounting.Application.Common.Interfaces;
using Accounting.Domain.Entity;
using Accounting.Domain.ValueObjects;
using FluentValidation;
using FluentValidation.Results;
using MediatR;
using Moq;

namespace Accounting.Application.Tests.Vouchers;

/// <summary>فاز ۵۲ — ذخیرهٔ اتمیک سند (ریسک #۲۱): ترتیب فرمان‌ها، تراکنش، کنترل هم‌زمانی و تراز.</summary>
public class SaveVoucherCommandHandlerTests
{
    private const string Vahed = "0101";
    private static readonly Guid NewHeadId = Guid.NewGuid();

    private readonly Mock<ISender> _sender = new();
    private readonly Mock<IUnitOfWork> _uow = new();
    private readonly Mock<IVoucherHeadRepository> _heads = new();
    private readonly Mock<IVoucherDetailRepository> _details = new();
    private readonly List<object> _sent = [];
    private TB_VOUCHERSHEAD _stored = new() { ID = NewHeadId, DOC_NUM = "000012", DOCLIFE = DocLife.Draft };

    public SaveVoucherCommandHandlerTests()
    {
        _sender.Setup(s => s.Send(It.IsAny<IRequest>(), It.IsAny<CancellationToken>()))
            .Callback<IRequest, CancellationToken>((r, _) => _sent.Add(r)).Returns(Task.CompletedTask);
        _sender.Setup(s => s.Send(It.IsAny<IRequest<Guid>>(), It.IsAny<CancellationToken>()))
            .Callback<IRequest<Guid>, CancellationToken>((r, _) => _sent.Add(r))
            .ReturnsAsync((IRequest<Guid> r, CancellationToken _) => r is CreateVoucherHeadCommand ? NewHeadId : Guid.NewGuid());
        _heads.Setup(h => h.GetForUpdateAsync(It.IsAny<Guid>(), Vahed, It.IsAny<CancellationToken>())).ReturnsAsync(() => _stored);
        Totals(100, 100, 2);
    }

    private void Totals(decimal d, decimal c, int n) =>
        _details.Setup(x => x.GetTotalsByHeadsAsync(It.IsAny<IReadOnlyCollection<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<Guid, VoucherTotals> { [NewHeadId] = new(d, c, n) });

    private SaveVoucherCommandHandler Handler() => new(_sender.Object, _uow.Object, _heads.Object, _details.Object);

    private static SaveVoucherHeadInput Head(DocLife? life = DocLife.Draft) =>
        new("12", "14050720", life, "شرح", null, null, null, "1405", null, null, null, null, null);

    private static SaveVoucherLineInput Line(Guid? id = null, decimal? debtor = 100, decimal? creditor = null) =>
        new(id, Guid.NewGuid(), null, null, null, null, "ردیف", 99, debtor, creditor);

    private static SaveVoucherCommand Cmd(Guid? headId, DocLife? life, IReadOnlyList<SaveVoucherLineInput> lines,
        IReadOnlyList<Guid>? deleted = null, VoucherConcurrencyToken? token = null) =>
        new(headId, Head(life), lines, deleted, token) { VahedCode = Vahed };

    private void VerifyCommitted()
    {
        _uow.Verify(u => u.BeginTransactionAsync(It.IsAny<CancellationToken>()), Times.Once);
        _uow.Verify(u => u.CommitTransactionAsync(It.IsAny<CancellationToken>()), Times.Once);
        _uow.Verify(u => u.RollbackTransactionAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    private void VerifyRolledBack()
    {
        _uow.Verify(u => u.RollbackTransactionAsync(It.IsAny<CancellationToken>()), Times.Once);
        _uow.Verify(u => u.CommitTransactionAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    // ─────────── سند جدید ───────────

    [Fact]
    public async Task New_CreatesDraftHead_ThenLinesRenumbered_ThenState()
    {
        _stored.DOCLIFE = DocLife.Temporary;
        var res = await Handler().Handle(Cmd(null, DocLife.Temporary, [Line(), Line(debtor: null, creditor: 100)]), default);

        Assert.Equal(NewHeadId, res.HeadId);
        var head = Assert.IsType<CreateVoucherHeadCommand>(_sent[0]);
        Assert.Equal(DocLife.Draft, head.DocLife); // وضعیت خواسته‌شده بعد از ردیف‌ها
        var l1 = Assert.IsType<CreateVoucherDetailCommand>(_sent[1]);
        var l2 = Assert.IsType<CreateVoucherDetailCommand>(_sent[2]);
        Assert.Equal((1, 2), (l1.Radif, l2.Radif));
        Assert.Equal(NewHeadId, l1.VoucherHeadId);
        var state = Assert.IsType<ChangeVoucherStateCommand>(_sent[3]);
        Assert.Equal(DocLife.Temporary, state.NewState);
        VerifyCommitted();
    }

    [Fact]
    public async Task New_Draft_DoesNotChangeState_NorCheckBalance()
    {
        Totals(100, 0, 1);
        await Handler().Handle(Cmd(null, DocLife.Draft, [Line()]), default);
        Assert.DoesNotContain(_sent, s => s is ChangeVoucherStateCommand);
        VerifyCommitted();
    }

    [Fact]
    public async Task New_UnbalancedNonDraft_RollsBack()
    {
        _stored.DOCLIFE = DocLife.Temporary;
        Totals(100, 90, 2);
        await Assert.ThrowsAsync<VoucherUnbalancedException>(() =>
            Handler().Handle(Cmd(null, DocLife.Temporary, [Line(), Line(debtor: null, creditor: 90)]), default));
        VerifyRolledBack();
    }

    [Fact]
    public async Task LineValidationError_IsRemappedToLineIndex_AndRollsBack()
    {
        _sender.Setup(s => s.Send(It.IsAny<IRequest<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((IRequest<Guid> r, CancellationToken _) => r is CreateVoucherDetailCommand
                ? throw new ValidationException([new ValidationFailure("Debtor", "مبلغ نامعتبر است")])
                : NewHeadId);

        var ex = await Assert.ThrowsAsync<ValidationException>(() => Handler().Handle(Cmd(null, DocLife.Draft, [Line()]), default));

        var e = Assert.Single(ex.Errors);
        Assert.Equal("Lines[0].Debtor", e.PropertyName);
        Assert.Equal("ردیف 1: مبلغ نامعتبر است", e.ErrorMessage);
        VerifyRolledBack();
    }

    // ─────────── ویرایش ───────────

    [Fact]
    public async Task Edit_DeletesFirst_ThenUpdatesOrCreatesLines_HeadLast()
    {
        var keep = Guid.NewGuid();
        var drop = Guid.NewGuid();

        await Handler().Handle(Cmd(NewHeadId, DocLife.Draft, [Line(keep), Line()], [drop, drop]), default);

        Assert.Equal(drop, Assert.IsType<DeleteVoucherDetailCommand>(_sent[0]).Id);
        Assert.Equal(keep, Assert.IsType<UpdateVoucherDetailCommand>(_sent[1]).Id);
        Assert.IsType<CreateVoucherDetailCommand>(_sent[2]);
        Assert.IsType<UpdateVoucherHeadCommand>(_sent[3]);
        Assert.Equal(4, _sent.Count); // حذف تکراری یک بار
        VerifyCommitted();
    }

    [Fact]
    public async Task Edit_StaleConcurrencyToken_IsConflict_BeforeAnyWrite()
    {
        _stored.UPDATEDDATE = new DateTime(2026, 10, 20, 10, 0, 5);
        var token = new VoucherConcurrencyToken(new DateTime(2026, 10, 20, 10, 0, 0));

        await Assert.ThrowsAsync<BusinessRuleException>(() => Handler().Handle(Cmd(NewHeadId, DocLife.Draft, [Line()], token: token), default));

        Assert.Empty(_sent);
        VerifyRolledBack();
    }

    [Fact]
    public async Task Edit_TokenComparedToTheSecond()
    {
        _stored.UPDATEDDATE = new DateTime(2026, 10, 20, 10, 0, 0).AddMilliseconds(700);
        var token = new VoucherConcurrencyToken(new DateTime(2026, 10, 20, 10, 0, 0));
        await Handler().Handle(Cmd(NewHeadId, DocLife.Draft, [Line()], token: token), default);
        VerifyCommitted();
    }

    [Fact]
    public async Task Edit_TemporaryVoucherMadeUnbalanced_RollsBack()
    {
        _stored.DOCLIFE = DocLife.Temporary;
        Totals(100, 50, 2);
        await Assert.ThrowsAsync<VoucherUnbalancedException>(() =>
            Handler().Handle(Cmd(NewHeadId, DocLife.Temporary, [Line(Guid.NewGuid())]), default));
        VerifyRolledBack();
    }

    [Fact]
    public async Task Edit_MissingHead_IsNotFound()
    {
        _heads.Setup(h => h.GetForUpdateAsync(It.IsAny<Guid>(), Vahed, It.IsAny<CancellationToken>())).ReturnsAsync((TB_VOUCHERSHEAD?)null);
        await Assert.ThrowsAsync<NotFoundException>(() =>
            Handler().Handle(Cmd(NewHeadId, DocLife.Draft, [Line()], token: new VoucherConcurrencyToken(null)), default));
        VerifyRolledBack();
    }

    // ─────────── اعتبارسنجی ───────────

    [Fact]
    public void Validator()
    {
        var v = new SaveVoucherCommandValidator();
        var id = Guid.NewGuid();
        Assert.False(v.Validate(Cmd(null, DocLife.Draft, [])).IsValid);
        Assert.False(v.Validate(Cmd(NewHeadId, DocLife.Draft, [Line(id), Line(id)])).IsValid);
        Assert.False(v.Validate(Cmd(null, DocLife.Draft, [Line()], [Guid.NewGuid()])).IsValid);
        Assert.False(v.Validate(Cmd(null, DocLife.Draft, Enumerable.Range(0, 1001).Select(_ => Line()).ToList())).IsValid);
        Assert.True(v.Validate(Cmd(NewHeadId, DocLife.Draft, [Line(), Line()], [Guid.NewGuid()])).IsValid);
    }
}
