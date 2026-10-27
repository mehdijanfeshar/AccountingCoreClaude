using Accounting.Application.Commands.Expenses.CreateExpense;
using Accounting.Application.Commands.Expenses.UpdateExpense;
using Accounting.Application.Common.Exceptions;
using Accounting.Application.Common.Interfaces;
using Accounting.Domain.Entity;
using Accounting.Domain.ValueObjects;
using Moq;

namespace Accounting.Application.Tests.Expenses.Commands;

/// <summary>مخزن کدینگ برای تست‌های هزینه: هر شناسه‌ای یک حساب از نوع داده‌شده (پیش‌فرض معین) است.</summary>
internal static class ExpenseTestAccounts
{
    public static IAccountCodeRepository Moin() => Of(TypeCodes.Moin);

    public static IAccountCodeRepository Of(TypeCodes? typeCode, bool exists = true)
    {
        var repository = new Mock<IAccountCodeRepository>();
        repository
            .Setup(r => r.GetForUpdateAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Guid id, CancellationToken _) =>
                exists ? new TB_ACCOUNTCODE { ID = id, TYPECODE = typeCode, ISDELETED = false } : null);
        return repository.Object;
    }
}

/// <summary>حساب هزینه فقط از سطح معین (صاحب پروژه ۲۰۲۶-۱۰-۲۶).</summary>
public class ExpenseAccountRuleTests
{
    private static Mock<ICurrentUser> CurrentUser()
    {
        var user = new Mock<ICurrentUser>();
        user.SetupGet(u => u.UserId).Returns("u1");
        return user;
    }

    private static CreateExpenseCommand Create(Guid? accountCodeId) =>
        new("0001", "هزینه", null, null, null, accountCodeId) { VahedCode = "0001" };

    [Theory]
    [InlineData(TypeCodes.Group)]
    [InlineData(TypeCodes.Kol)]
    public async Task Create_WithNonMoinAccount_Throws(TypeCodes typeCode)
    {
        var repository = new Mock<IExpenseRepository>();
        var handler = new CreateExpenseCommandHandler(
            repository.Object, new Mock<IUnitOfWork>().Object, CurrentUser().Object, ExpenseTestAccounts.Of(typeCode));

        await Assert.ThrowsAsync<BusinessRuleException>(() => handler.Handle(Create(Guid.NewGuid()), CancellationToken.None));
        repository.Verify(r => r.AddAsync(It.IsAny<TB_EXPENCE>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Create_WithMissingAccount_Throws()
    {
        var handler = new CreateExpenseCommandHandler(
            new Mock<IExpenseRepository>().Object, new Mock<IUnitOfWork>().Object, CurrentUser().Object,
            ExpenseTestAccounts.Of(TypeCodes.Moin, exists: false));

        await Assert.ThrowsAsync<BusinessRuleException>(() => handler.Handle(Create(Guid.NewGuid()), CancellationToken.None));
    }

    [Fact]
    public async Task Create_WithoutAccount_IsAllowed()
    {
        var repository = new Mock<IExpenseRepository>();
        var handler = new CreateExpenseCommandHandler(
            repository.Object, new Mock<IUnitOfWork>().Object, CurrentUser().Object, ExpenseTestAccounts.Of(TypeCodes.Kol));

        await handler.Handle(Create(null), CancellationToken.None);

        repository.Verify(r => r.AddAsync(It.IsAny<TB_EXPENCE>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Update_ChangingToKolAccount_Throws()
    {
        var id = Guid.NewGuid();
        var repository = new Mock<IExpenseRepository>();
        repository.Setup(r => r.GetForUpdateAsync(id, "0001", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new TB_EXPENCE { ID = id, VAHEDCODE = "0001", ACCOUNTCODE_ID = Guid.NewGuid(), ISDELETED = false });
        var handler = new UpdateExpenseCommandHandler(
            repository.Object, new Mock<IUnitOfWork>().Object, CurrentUser().Object, ExpenseTestAccounts.Of(TypeCodes.Kol));

        var command = new UpdateExpenseCommand(id, "0001", "هزینه", null, null, null, Guid.NewGuid()) { VahedCode = "0001" };

        await Assert.ThrowsAsync<BusinessRuleException>(() => handler.Handle(command, CancellationToken.None));
    }

    [Fact]
    public async Task Update_KeepingExistingLegacyAccount_IsNotRechecked()
    {
        var id = Guid.NewGuid();
        var accountId = Guid.NewGuid();
        var repository = new Mock<IExpenseRepository>();
        repository.Setup(r => r.GetForUpdateAsync(id, "0001", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new TB_EXPENCE { ID = id, VAHEDCODE = "0001", ACCOUNTCODE_ID = accountId, ISDELETED = false });
        repository.Setup(r => r.GetActiveTafsiliLinksAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<TB_EXPENCE_LINK_TAFSILI>());
        var handler = new UpdateExpenseCommandHandler(
            repository.Object, new Mock<IUnitOfWork>().Object, CurrentUser().Object, ExpenseTestAccounts.Of(TypeCodes.Kol));

        await handler.Handle(new UpdateExpenseCommand(id, "0001", "هزینه", null, null, null, accountId) { VahedCode = "0001" }, CancellationToken.None);
    }
}
