using Accounting.Application.Common.Exceptions;

namespace Accounting.Application.Vouchers.Commands.Common;

/// <summary>
/// Enforces «تفصیلی الزامی» on the voucher write path. See
/// <see cref="VoucherTafsiliLevelGuard"/> for the rule, its two halves and why it is a service
/// rather than a FluentValidation validator.
///
/// <b>The interface exists so that tests about something else can say so.</b> Most voucher
/// handler tests are about audit stamping, ordering or reconcile logic; forcing each of them to
/// configure a معین's levels first would bury what they actually assert, and a test that has to
/// set up an unrelated rule tends to be changed to match the code rather than the other way
/// round. The rule gets its own tests instead, and each handler gets one test proving it calls
/// this — so "forgot to enforce it" still fails, which is the only thing the interface could
/// otherwise have made easy to miss. Production wiring is unaffected: the constructor dependency
/// is required, so a handler cannot be built without one.
/// </summary>
public interface IVoucherTafsiliLevelGuard
{
    /// <summary>
    /// Throws when <paramref name="tafsiliLinks"/> is not a valid تفصیلی assignment for
    /// <paramref name="accountCodeId"/>.
    /// </summary>
    /// <param name="accountCodeId">
    /// The line's <c>ACCOUNT_ID</c>. <see langword="null"/> is allowed — the column is nullable, and
    /// a line with no حساب has nothing to require. Any تفصیلی sent for such a line is still
    /// rejected: there is no configuration that could permit it.
    /// </param>
    /// <param name="tafsiliLinks">
    /// The تفصیلی the line will have <b>after</b> this request, not the delta. Callers on the
    /// update path have to resolve that themselves, because a null list there means "leave the
    /// existing assignments alone" rather than "no assignments".
    /// </param>
    /// <exception cref="RequiredTafsiliLevelMissingException">A configured level has no value.</exception>
    /// <exception cref="TafsiliLevelNotPermittedException">A value was sent for an unconfigured level.</exception>
    Task EnsureSatisfiedAsync(
        Guid? accountCodeId,
        IReadOnlyCollection<VoucherDetailTafsiliLinkInput> tafsiliLinks,
        CancellationToken cancellationToken = default);
}
