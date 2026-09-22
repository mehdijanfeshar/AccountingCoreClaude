using Accounting.Application.Common.Interfaces;
using Accounting.Domain.Entity;
using Moq;

namespace Accounting.Application.Tests.Accounts.Commands.Common;

/// <summary>
/// Stubs the two reads <c>AccountLevelLinkSynchronizer</c> makes, for the tests of the three
/// «ارتباط معین با گروه تفصیلی» handlers, which are about those handlers rather than about the
/// sync.
///
/// <b>Needed because Moq returns <see langword="null"/>, not an empty list, for an unstubbed
/// <c>Task&lt;IReadOnlyList&lt;T&gt;&gt;</c>.</b> Defending against that null in the synchronizer
/// itself would be the wrong fix: the real repository always returns a list, so a null there means
/// a broken test double, and swallowing it would hide exactly that.
/// </summary>
internal static class AccountLevelLinkSyncStubs
{
    public static Mock<IAccountCodeRepository> WithNoExistingLevelLinks(this Mock<IAccountCodeRepository> repository)
    {
        repository
            .Setup(r => r.GetTafsilGroupLinksForSyncAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<TB_ACCOUNT_LINK_TAFSILGROUP>());
        repository
            .Setup(r => r.GetLevelLinksForSyncAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<TB_ACCOUNT_LINK_LEVEL>());

        return repository;
    }
}
