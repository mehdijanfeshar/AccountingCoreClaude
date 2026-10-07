using Accounting.Application.Common.Behaviors;
using Accounting.Application.Common.Exceptions;
using Accounting.Application.Common.Interfaces;
using Accounting.Application.Common.Security;
using Accounting.Application.Vouchers.RoleMenuAccessTestDoubles;
using MediatR;
using Moq;

namespace Accounting.Application.Vouchers.RoleMenuAccessTestDoubles
{
    // ماژول از namespace درخواست خوانده می‌شود ⇒ این‌ها درخواست‌های ماژول «Vouchers»اند.
    public sealed record FakeVoucherCommand : IRequest<int>;

    public sealed record FakeVoucherQuery : IRequest<int>;
}

namespace Accounting.Application.Tests.Common.Behaviors
{
    /// <summary>فاز ۵۴ — RoleAuthorizationBehavior وقتی «دسترسی نقش‌ها» پیکربندی شده است.</summary>
    public sealed class RoleMenuAccessBehaviorTests
    {
        private const string Voucher = "/operation/voucher-heads";
        private const string VoucherNew = "/operation/vouchers/new";

        private static Mock<ICurrentUser> User(params string[] roles)
        {
            var user = new Mock<ICurrentUser>();
            user.Setup(u => u.IsInRole(It.IsAny<string>())).Returns((string r) => roles.Contains(r));
            user.SetupGet(u => u.AllRoles).Returns(roles);
            return user;
        }

        private static IRoleMenuAccessStore Store(RoleMenuAccessSnapshot? snapshot)
        {
            var store = new Mock<IRoleMenuAccessStore>();
            store.Setup(s => s.GetSnapshotAsync(It.IsAny<CancellationToken>())).ReturnsAsync(snapshot);
            return store.Object;
        }

        private static RoleMenuAccessSnapshot Snapshot(params (string Role, string Menu, int Level)[] rows) => new(rows);

        private static Task<int> Run<TRequest>(TRequest request, Mock<ICurrentUser> user, IRoleMenuAccessStore? store)
            where TRequest : IRequest<int>
            => new RoleAuthorizationBehavior<TRequest, int>(user.Object, store)
                .Handle(request, _ => Task.FromResult(42), CancellationToken.None);

        [Fact]
        public async Task NotConfigured_FallsBackToFixedRules()
        {
            // جدول خالی ⇒ کارمند حسابداری مثل قبل سند ثبت می‌کند و گزارش‌گیر نه.
            Assert.Equal(42, await Run(new FakeVoucherCommand(), User(AppRoles.User), Store(null)));
            await Assert.ThrowsAsync<RoleAccessDeniedException>(() => Run(new FakeVoucherCommand(), User(AppRoles.Report), Store(null)));
        }

        [Fact]
        public async Task Configured_EditOnAnyMenuOfTheModule_AllowsWrite()
        {
            var snapshot = Snapshot(("FINANCIAL CORE REPORT", Voucher, RoleMenuAccessLevels.View), ("FINANCIAL CORE REPORT", VoucherNew, RoleMenuAccessLevels.Edit));
            Assert.Equal(42, await Run(new FakeVoucherCommand(), User(AppRoles.Report), Store(snapshot)));
        }

        [Fact]
        public async Task Configured_ViewOnly_BlocksWrite_ButAllowsRead()
        {
            var snapshot = Snapshot((AppRoles.User, Voucher, RoleMenuAccessLevels.View), (AppRoles.User, VoucherNew, RoleMenuAccessLevels.View));
            await Assert.ThrowsAsync<RoleAccessDeniedException>(() => Run(new FakeVoucherCommand(), User(AppRoles.User), Store(snapshot)));
            Assert.Equal(42, await Run(new FakeVoucherQuery(), User(AppRoles.User), Store(snapshot)));
        }

        [Fact]
        public async Task Configured_CustomRoleFromKeycloak_IsRecognised()
        {
            var snapshot = Snapshot(("ACCOUNTING CLERK", VoucherNew, RoleMenuAccessLevels.Edit));
            Assert.Equal(42, await Run(new FakeVoucherCommand(), User("ACCOUNTING CLERK"), Store(snapshot)));
            Assert.Equal(42, await Run(new FakeVoucherQuery(), User("ACCOUNTING CLERK"), Store(snapshot)));
        }

        [Fact]
        public async Task Configured_UserWithNoKnownRole_IsDeniedEvenForReads()
        {
            var snapshot = Snapshot(("ACCOUNTING CLERK", VoucherNew, RoleMenuAccessLevels.Edit));
            await Assert.ThrowsAsync<RoleAccessDeniedException>(() => Run(new FakeVoucherQuery(), User("SOMETHING ELSE"), Store(snapshot)));
        }

        [Fact]
        public async Task SetadAdmin_AlwaysAllowed_EvenWithEverythingSetToNone()
        {
            var snapshot = Snapshot((AppRoles.SetadAdmin, VoucherNew, RoleMenuAccessLevels.None));
            Assert.Equal(42, await Run(new FakeVoucherCommand(), User(AppRoles.SetadAdmin), Store(snapshot)));
        }

        [Fact]
        public void Defaults_ReproduceThePreviousFixedBehaviour()
        {
            var voucherNew = MenuCatalog.All.Single(m => m.Key == VoucherNew);
            var accountCodes = MenuCatalog.All.Single(m => m.Key == "/base/account-codes");
            var trial = MenuCatalog.All.Single(m => m.Key == "/reports/trial-balance");

            Assert.Equal(RoleMenuAccessLevels.Edit, MenuCatalog.DefaultLevel(AppRoles.User, voucherNew));
            Assert.Equal(RoleMenuAccessLevels.None, MenuCatalog.DefaultLevel(AppRoles.Report, voucherNew));
            // کدینگ: دادهٔ پایه فقط مدیر ستاد می‌نویسد؛ بقیهٔ نقش‌های عملیاتی فقط می‌بینند، سطح کشور هم می‌بیند.
            Assert.Equal(RoleMenuAccessLevels.View, MenuCatalog.DefaultLevel(AppRoles.MaliAdmin, accountCodes));
            Assert.Equal(RoleMenuAccessLevels.View, MenuCatalog.DefaultLevel(AppRoles.National, accountCodes));
            Assert.Equal(RoleMenuAccessLevels.None, MenuCatalog.DefaultLevel(AppRoles.Report, accountCodes));
            // گزارش: همه می‌بینند، هیچ‌کس «ثبت و تغییر» ندارد.
            Assert.Equal(RoleMenuAccessLevels.View, MenuCatalog.DefaultLevel(AppRoles.Report, trial));
        }

        private static HeadquartersAccessService Hq(Mock<ICurrentUser> user, RoleMenuAccessSnapshot? snapshot, bool userUnitIsHq = false)
        {
            user.SetupGet(u => u.VahedCode).Returns(userUnitIsHq ? "0000" : "1155");
            var units = new Mock<IUnitAccessReadRepository>();
            units.Setup(u => u.GetUnitProfileAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((string code, CancellationToken _) => new UnitProfile("x", IsHeadquarters: code == "0000"));
            return new HeadquartersAccessService(user.Object, units.Object, Store(snapshot));
        }

        [Fact]
        public async Task Ability_NotConfigured_FollowsDefaultRule()
        {
            // گروه واحد در گزارش: سطح کشور دارد؛ تعیین دامنهٔ تفصیلی: فقط ستاد مرکزی.
            Assert.True(await Hq(User(AppRoles.National), null).HasAbilityAsync(AbilityCatalog.ReportsUnitCategory));
            Assert.False(await Hq(User(AppRoles.National), null).HasAbilityAsync(AbilityCatalog.TafsiliScope));
            Assert.False(await Hq(User(AppRoles.SetadAdmin), null, userUnitIsHq: false).HasAbilityAsync(AbilityCatalog.TafsiliScope));
            Assert.True(await Hq(User(AppRoles.SetadAdmin), null, userUnitIsHq: true).HasAbilityAsync(AbilityCatalog.TafsiliScope));
        }

        [Fact]
        public async Task Ability_Configured_GrantedPerRoleFromTable()
        {
            var key = AbilityCatalog.StorageKey(AbilityCatalog.TafsiliScope);
            var snapshot = Snapshot((AppRoles.MaliAdmin, key, RoleMenuAccessLevels.View), (AppRoles.National, key, RoleMenuAccessLevels.None));
            Assert.True(await Hq(User(AppRoles.MaliAdmin), snapshot).HasAbilityAsync(AbilityCatalog.TafsiliScope));
            Assert.False(await Hq(User(AppRoles.National), snapshot).HasAbilityAsync(AbilityCatalog.ReportsUnitCategory));
            // مدیر ستاد مرکزی همیشه.
            Assert.True(await Hq(User(AppRoles.SetadAdmin), snapshot, userUnitIsHq: true).HasAbilityAsync(AbilityCatalog.ReportsUnitCategory));
        }

        [Fact]
        public void EveryCatalogModule_IsARealApplicationModule()
        {
            var modules = typeof(AppRoles).Assembly.GetTypes()
                .Select(t => t.Namespace?.Split('.'))
                .Where(p => p is { Length: > 2 } && p[0] == "Accounting" && p[1] == "Application")
                .Select(p => p![2])
                .ToHashSet(StringComparer.Ordinal);

            var unknown = MenuCatalog.All.SelectMany(m => m.Modules).Distinct().Where(m => !modules.Contains(m)).ToList();
            Assert.True(unknown.Count == 0, "Unknown modules in MenuCatalog: " + string.Join(", ", unknown));
        }
    }
}
