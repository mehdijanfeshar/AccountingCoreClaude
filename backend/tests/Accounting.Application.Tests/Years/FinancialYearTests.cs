using System.Globalization;
using Accounting.Application.Commands.Years.CreateFinancialYear;
using Accounting.Application.Common.Exceptions;
using Accounting.Application.Common.Interfaces;
using Accounting.Application.Common.Security;
using Accounting.Application.Queries.SysTypes;
using Accounting.Application.Queries.SysTypes.GetSysTypes;
using Accounting.Application.Queries.Years;
using Accounting.Application.Queries.Years.GetYears;
using Accounting.Domain.Entity;
using Moq;

namespace Accounting.Application.Tests.Years;

public class FinancialYearTests
{
    private readonly Mock<IFinancialYearRepository> _repo = new();
    private readonly Mock<IHeadquartersAccessService> _hq = new();
    private readonly Mock<IUnitOfWork> _uow = new();
    private readonly Mock<ICurrentUser> _user = new();

    public FinancialYearTests()
    {
        _user.Setup(u => u.UserId).Returns("u1");
        _repo.Setup(r => r.GetAllForUpdateAsync(It.IsAny<CancellationToken>())).ReturnsAsync(new List<TB_YEAR>());
        _repo.Setup(r => r.GetActiveBankAccountsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<(Guid, string)>());
        _repo.Setup(r => r.GetSoriCheckBooksOfYearForUpdateAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<TB_CHECKBOOK>());
    }

    private static (int Year, int Month) PersianNow()
    {
        var c = new PersianCalendar();
        var n = DateTime.Now;
        return (c.GetYear(n), c.GetMonth(n));
    }

    [Fact]
    public async Task Create_NotHeadquartersAdmin_Throws_NothingSaved()
    {
        var h = new CreateFinancialYearCommandHandler(_repo.Object, _hq.Object, _uow.Object, _user.Object);
        await Assert.ThrowsAsync<RoleAccessDeniedException>(() => h.Handle(new CreateFinancialYearCommand(), default));
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        _repo.Verify(r => r.Add(It.IsAny<TB_YEAR>()), Times.Never);
    }

    [Fact]
    public async Task Status_NotHeadquartersAdmin_Throws()
    {
        var h = new GetFinancialYearCreationStatusQueryHandler(_repo.Object, _hq.Object);
        await Assert.ThrowsAsync<RoleAccessDeniedException>(() => h.Handle(new GetFinancialYearCreationStatusQuery(), default));
    }

    [Fact]
    public async Task Status_ReflectsCalendarMonthAndExistingYears()
    {
        _hq.Setup(h => h.IsHeadquartersAdminAsync(It.IsAny<CancellationToken>())).ReturnsAsync(true);
        var (year, month) = PersianNow();
        _repo.Setup(r => r.GetAllForUpdateAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<TB_YEAR> { new() { WORKING_YEAR = (short)(year - 1), ISCURRENT = true } });
        _repo.Setup(r => r.GetActiveBankAccountsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<(Guid, string)> { (Guid.NewGuid(), "0002") });

        var s = await new GetFinancialYearCreationStatusQueryHandler(_repo.Object, _hq.Object)
            .Handle(new GetFinancialYearCreationStatusQuery(), default);

        Assert.Equal(year, s.CandidateYear);
        Assert.Equal(year - 1, s.CurrentYear);
        Assert.Equal(1, s.BankAccounts);
        // فقط در فروردین مجاز است
        Assert.Equal(month == 1, s.CanCreate);
        Assert.Equal(month != 1, s.Reason is not null);
    }

    [Fact]
    public async Task Create_OutsideFarvardin_ThrowsBusinessRule_NothingChanged()
    {
        var (_, month) = PersianNow();
        if (month == 1) return; // در فروردین این شاخه قابل اجرا نیست؛ تست زمان‌وابسته است.
        _hq.Setup(h => h.IsHeadquartersAdminAsync(It.IsAny<CancellationToken>())).ReturnsAsync(true);
        var years = new List<TB_YEAR> { new() { WORKING_YEAR = 1400, ISCURRENT = true } };
        _repo.Setup(r => r.GetAllForUpdateAsync(It.IsAny<CancellationToken>())).ReturnsAsync(years);

        var h = new CreateFinancialYearCommandHandler(_repo.Object, _hq.Object, _uow.Object, _user.Object);
        await Assert.ThrowsAsync<BusinessRuleException>(() => h.Handle(new CreateFinancialYearCommand(), default));

        Assert.True(years[0].ISCURRENT);
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Create_InFarvardin_AddsYearAndSoriCheckBooks()
    {
        var (year, month) = PersianNow();
        if (month != 1) return; // فقط در فروردین اجرا می‌شود.
        _hq.Setup(h => h.IsHeadquartersAdminAsync(It.IsAny<CancellationToken>())).ReturnsAsync(true);
        var old = new TB_YEAR { WORKING_YEAR = (short)(year - 1), ISCURRENT = true };
        _repo.Setup(r => r.GetAllForUpdateAsync(It.IsAny<CancellationToken>())).ReturnsAsync(new List<TB_YEAR> { old });
        _repo.Setup(r => r.GetActiveBankAccountsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<(Guid, string)> { (Guid.NewGuid(), "0002") });

        var r = await new CreateFinancialYearCommandHandler(_repo.Object, _hq.Object, _uow.Object, _user.Object)
            .Handle(new CreateFinancialYearCommand(), default);

        Assert.Equal(year, r.Year);
        Assert.Equal(year - 1, r.PreviousYear);
        Assert.Equal(1, r.CheckBooksCreated);
        Assert.False(old.ISCURRENT);
        _repo.Verify(x => x.Add(It.Is<TB_YEAR>(y => y.WORKING_YEAR == year && y.ISCURRENT == true)), Times.Once);
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    // ---------------- GetYears / GetSysTypes: واگذاری مستقیم به مخزن
    [Fact]
    public async Task GetYears_DelegatesToRepository()
    {
        var repo = new Mock<IYearReadRepository>();
        IReadOnlyList<YearDto> data = new List<YearDto> { new(1405, true, 1) };
        repo.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync(data);
        var res = await new GetYearsQueryHandler(repo.Object).Handle(new GetYearsQuery(), default);
        Assert.Same(data, res);
    }

    [Fact]
    public async Task GetSysTypes_DelegatesToRepository()
    {
        var repo = new Mock<ISysTypeReadRepository>();
        IReadOnlyList<SysTypeDto> data = new List<SysTypeDto> { new(Guid.NewGuid(), "1", "عمومی") };
        repo.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync(data);
        var res = await new GetSysTypesQueryHandler(repo.Object).Handle(new GetSysTypesQuery(), default);
        Assert.Same(data, res);
    }
}
