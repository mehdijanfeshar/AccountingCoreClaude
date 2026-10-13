using Accounting.Application.Common.Exceptions;
using Accounting.Application.Common.Interfaces;
using Accounting.Application.RabetClosings;
using Accounting.Domain.Entity;
using Accounting.Domain.ValueObjects;
using Moq;

namespace Accounting.Application.Tests.RabetClosings;

public class RabetClosingTests
{
    private readonly Mock<IRabetClosingRepository> _repo = new();
    private readonly Mock<ICurrentUser> _user = new();
    private readonly Mock<IUnitOfWork> _uow = new();
    private static readonly Guid T1 = Guid.NewGuid(), T2 = Guid.NewGuid();
    private static readonly Guid Rabet = Guid.NewGuid(), A1 = Guid.NewGuid(), A2 = Guid.NewGuid();
    private List<TB_RABET_CLOSING> _added = new();

    public RabetClosingTests()
    {
        _user.Setup(u => u.UserId).Returns("u1");
        _repo.Setup(r => r.GetExistingPairsAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<(Guid, Guid)>());
        _repo.Setup(r => r.GetExistingVahedTypeIdsAsync(It.IsAny<IReadOnlyCollection<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyCollection<Guid> ids, CancellationToken _) => ids.ToList());
        _repo.Setup(r => r.AddRangeAsync(It.IsAny<IEnumerable<TB_RABET_CLOSING>>(), It.IsAny<CancellationToken>()))
            .Callback<IEnumerable<TB_RABET_CLOSING>, CancellationToken>((rows, _) => _added = rows.ToList());
    }

    private static RabetClosingAccount A(Guid id, string code, TypeCodes? t = null) => new(id, code, null, t);
    private void Accounts(params RabetClosingAccount[] a)
        => _repo.Setup(r => r.GetAccountsAsync(It.IsAny<IReadOnlyCollection<Guid>>(), It.IsAny<CancellationToken>())).ReturnsAsync(a.ToList());

    private CreateRabetClosingsHandler H() => new(_repo.Object, _user.Object, _uow.Object);
    private static CreateRabetClosingsCommand Cmd(IReadOnlyList<Guid>? types = null, IReadOnlyList<Guid>? accs = null, Guid? rabet = null)
        => new("1404", types ?? [T1], accs ?? [A1], rabet ?? Rabet, " عنوان ");

    [Fact]
    public void Validator_RejectsBadInput()
    {
        var v = new CreateRabetClosingsValidator();
        Assert.True(v.Validate(Cmd()).IsValid);
        Assert.False(v.Validate(Cmd() with { Year = "99" }).IsValid);
        Assert.False(v.Validate(Cmd() with { VahedTypeIds = [] }).IsValid);
        Assert.False(v.Validate(Cmd() with { AccountIds = [] }).IsValid);
        Assert.False(v.Validate(Cmd() with { RabetAccountId = Guid.Empty }).IsValid);
        Assert.False(v.Validate(Cmd() with { Title = "" }).IsValid);
        Assert.False(v.Validate(Cmd() with { Title = new string('x', 201) }).IsValid);
    }

    [Fact]
    public async Task Create_AccountSameAsRabet_Throws()
        => await Assert.ThrowsAsync<BusinessRuleException>(() => H().Handle(Cmd(accs: [Rabet]), default));

    [Fact]
    public async Task Create_RabetNotFound_Throws()
    {
        Accounts(A(A1, "6101"));
        await Assert.ThrowsAsync<BusinessRuleException>(() => H().Handle(Cmd(), default));
    }

    [Fact]
    public async Task Create_RabetNotMoein_Throws()
    {
        Accounts(A(Rabet, "3101"), A(A1, "6101"));
        await Assert.ThrowsAsync<BusinessRuleException>(() => H().Handle(Cmd(), default));
    }

    [Fact]
    public async Task Create_AccountNotFound_Throws()
    {
        Accounts(A(Rabet, "310101"));
        await Assert.ThrowsAsync<BusinessRuleException>(() => H().Handle(Cmd(), default));
    }

    [Fact]
    public async Task Create_AccountWithBadCodeLength_Throws()
    {
        Accounts(A(Rabet, "310101"), A(A1, "61010"));
        await Assert.ThrowsAsync<BusinessRuleException>(() => H().Handle(Cmd(), default));
    }

    [Fact]
    public async Task Create_UnknownVahedType_Throws()
    {
        Accounts(A(Rabet, "310101"), A(A1, "6101"));
        _repo.Setup(r => r.GetExistingVahedTypeIdsAsync(It.IsAny<IReadOnlyCollection<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Guid>());
        await Assert.ThrowsAsync<BusinessRuleException>(() => H().Handle(Cmd(), default));
    }

    [Fact]
    public async Task Create_Duplicate_Throws_NothingSaved()
    {
        Accounts(A(Rabet, "310101"), A(A1, "6101"));
        _repo.Setup(r => r.GetExistingPairsAsync("1404", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<(Guid, Guid)> { (T1, A1) });
        await Assert.ThrowsAsync<BusinessRuleException>(() => H().Handle(Cmd(), default));
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Create_CrossProduct_LevelsFromCodeLengthAndTypeCode()
    {
        var group = Guid.NewGuid();
        Accounts(A(Rabet, "310101"), A(A1, "6101"), A(A2, "610101"), A(group, "6", TypeCodes.Group));

        var n = await H().Handle(Cmd(types: [T1, T2, T1], accs: [A1, A2, group, A1]), default);

        Assert.Equal(6, n);
        Assert.Equal(6, _added.Count);
        Assert.Equal(RabetAccountLevel.Kol, _added.First(r => r.ACCOUNTCODE_ID == A1).TYPEACCOUNTCODE);
        Assert.Equal(RabetAccountLevel.Moein, _added.First(r => r.ACCOUNTCODE_ID == A2).TYPEACCOUNTCODE);
        Assert.Equal(RabetAccountLevel.Group, _added.First(r => r.ACCOUNTCODE_ID == group).TYPEACCOUNTCODE);
        Assert.All(_added, r =>
        {
            Assert.Equal("عنوان", r.TITLE);
            Assert.Equal("1404", r.YEAR);
            Assert.Equal("u1", r.ADDUSERID);
            Assert.Equal(false, r.ISDELETED);
            Assert.Equal(Rabet, r.ACCOUNTCODE_RABET_ID);
        });
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    // ---------------- Delete
    private DeleteRabetClosingHandler D() => new(_repo.Object, _user.Object, _uow.Object);

    [Fact]
    public async Task Delete_NotFound_Throws()
        => await Assert.ThrowsAsync<NotFoundException>(() => D().Handle(new(Guid.NewGuid()), default));

    [Fact]
    public async Task Delete_AlreadyDeleted_IsNoOp()
    {
        var id = Guid.NewGuid();
        _repo.Setup(r => r.GetForUpdateAsync(id, It.IsAny<CancellationToken>())).ReturnsAsync(new TB_RABET_CLOSING { ID = id, ISDELETED = true });
        await D().Handle(new(id), default);
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Delete_ClosingVoucherIssued_Throws()
    {
        var id = Guid.NewGuid();
        var row = new TB_RABET_CLOSING { ID = id, YEAR = "1404", VAHEDTYPE_ID = T1 };
        _repo.Setup(r => r.GetForUpdateAsync(id, It.IsAny<CancellationToken>())).ReturnsAsync(row);
        _repo.Setup(r => r.CountClosingVouchersAsync("1404", T1, It.IsAny<CancellationToken>())).ReturnsAsync(1);
        await Assert.ThrowsAsync<BusinessRuleException>(() => D().Handle(new(id), default));
        Assert.NotEqual(true, row.ISDELETED);
    }

    [Fact]
    public async Task Delete_NoVoucher_SoftDeletes()
    {
        var id = Guid.NewGuid();
        var row = new TB_RABET_CLOSING { ID = id, YEAR = "1404", VAHEDTYPE_ID = T1 };
        _repo.Setup(r => r.GetForUpdateAsync(id, It.IsAny<CancellationToken>())).ReturnsAsync(row);
        await D().Handle(new(id), default);
        Assert.True(row.ISDELETED);
        Assert.Equal("u1", row.CHANGEUSERID);
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}
