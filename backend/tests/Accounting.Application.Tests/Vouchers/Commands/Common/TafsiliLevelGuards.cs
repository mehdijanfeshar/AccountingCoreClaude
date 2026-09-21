using Accounting.Application.Vouchers.Commands.Common;
using Moq;

namespace Accounting.Application.Tests.Vouchers.Commands.Common;

/// <summary>
/// Test doubles for <see cref="IVoucherTafsiliLevelGuard"/>.
///
/// <b><see cref="Permissive"/> is for tests about something else.</b> A test asserting how audit
/// columns are stamped, or in what order rows are staged, should not first have to configure a
/// معین's تفصیلی levels — that setup would bury the assertion and, worse, would have to be
/// re-tuned every time the rule changed, which is how a test quietly stops testing anything.
///
/// The rule itself is covered by <c>VoucherTafsiliLevelGuardTests</c>, and each handler has a test
/// proving it actually calls the guard and stages nothing when the guard rejects — so a handler
/// that skipped enforcement would still fail, which is the one thing a permissive double could
/// otherwise have hidden.
/// </summary>
internal static class TafsiliLevelGuards
{
    /// <summary>A guard that accepts everything. Use when the test is not about the rule.</summary>
    public static IVoucherTafsiliLevelGuard Permissive() => PermissiveMock().Object;

    /// <summary>
    /// The same double, but as the <see cref="Mock{T}"/> — for the handler tests that assert the
    /// guard was invoked with the line's own حساب and تفصیلی.
    /// </summary>
    public static Mock<IVoucherTafsiliLevelGuard> PermissiveMock()
    {
        var guard = new Mock<IVoucherTafsiliLevelGuard>();
        guard
            .Setup(g => g.EnsureSatisfiedAsync(
                It.IsAny<Guid?>(),
                It.IsAny<IReadOnlyCollection<VoucherDetailTafsiliLinkInput>>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        return guard;
    }

    /// <summary>A guard that rejects everything — for proving a handler stages nothing when it throws.</summary>
    public static IVoucherTafsiliLevelGuard Rejecting(Exception exception)
    {
        var guard = new Mock<IVoucherTafsiliLevelGuard>();
        guard
            .Setup(g => g.EnsureSatisfiedAsync(
                It.IsAny<Guid?>(),
                It.IsAny<IReadOnlyCollection<VoucherDetailTafsiliLinkInput>>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(exception);

        return guard.Object;
    }
}
