using Accounting.Application.Common.Exceptions;
using Accounting.Infrastructure.Repositories;

namespace Accounting.Infrastructure.Tests.Repositories;

/// <summary>
/// Direct tests for the one place record-level unit ownership is decided. Every by-id repository
/// method routes its answer through <see cref="VahedOwnership.EnsureOwned"/>, so these four cases
/// are the whole rule — the repository tests around them only prove the call is wired up.
/// </summary>
public sealed class VahedOwnershipTests
{
    private static readonly Guid Id = Guid.Parse("11111111-1111-1111-1111-111111111111");

    [Fact]
    public void SameUnit_IsAllowed()
    {
        VahedOwnership.EnsureOwned(ownerVahedCode: "0042", callerVahedCode: "0042", Id, "WorkShop");
    }

    [Fact]
    public void DifferentUnit_IsDenied()
    {
        var exception = Assert.Throws<UnitAccessDeniedException>(
            () => VahedOwnership.EnsureOwned(
                ownerVahedCode: "0042",
                callerVahedCode: "0043",
                Id,
                "WorkShop"));

        // The owning unit belongs in the log, so it is in the exception message. It must not reach
        // the client — GlobalExceptionHandler builds the 403 body from a fixed string and never
        // from this message; GlobalExceptionHandlerTests holds that end of the contract.
        Assert.Contains("0042", exception.Message, StringComparison.Ordinal);
        Assert.Contains("0043", exception.Message, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void BlankOwner_IsAllowed_BecauseItMarksAGloballySharedRow(string ownerVahedCode)
    {
        // The reference project writes VahedCode = "" for Owner = Global tafsili records. Reading
        // blank as "owned by nobody, therefore denied" would silently make every global record
        // unreachable from every unit — a data outage dressed up as a security check.
        VahedOwnership.EnsureOwned(ownerVahedCode, callerVahedCode: "0042", Id, "Tafsili");
    }

    [Fact]
    public void NullOwner_IsAllowed_BecauseItMeansNoRowWasFound()
    {
        // Callers pass entity?.VAHEDCODE, so null means the lookup found nothing. "Missing" is the
        // handler's business (NotFoundException → 404), not an access decision; throwing 403 here
        // would turn every genuine typo into a misleading "not your unit".
        VahedOwnership.EnsureOwned(ownerVahedCode: null, callerVahedCode: "0042", Id, "WorkShop");
    }

    [Fact]
    public void UnitComparison_IsCaseSensitive()
    {
        // Ordinal, not culture- or case-insensitive: VAHEDCODE is a 4-character unit code, and
        // letting "004a" match "004A" would widen access on a technicality rather than a decision.
        Assert.Throws<UnitAccessDeniedException>(
            () => VahedOwnership.EnsureOwned(
                ownerVahedCode: "004a",
                callerVahedCode: "004A",
                Id,
                "WorkShop"));
    }
}
