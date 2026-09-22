using Accounting.Application.Common.Interfaces;
using Accounting.Domain.Entity;
using Accounting.Domain.ValueObjects;
using Moq;

namespace Accounting.Application.Tests.TestSupport;

/// <summary>
/// Parent-voucher stubs for the two detail handlers, which since phase 38 must consult the head's
/// <c>DOCLIFE</c> before changing a line (<c>VoucherEditability</c>).
/// </summary>
internal static class VoucherHeadStubs
{
    /// <summary>
    /// A head repository that finds nothing.
    ///
    /// <para>
    /// This is what the pre-existing detail-handler tests use, and it is the honest default for
    /// them: they were written to exercise soft-delete/field-copy mechanics on a line, say nothing
    /// about parent state, and their fixtures never set up a parent row. The guard skips a parent
    /// it cannot load, so those tests keep testing exactly what they always did. The editability
    /// rule itself is covered separately by <c>VoucherEditabilityTests</c> and the dedicated
    /// handler tests, not by silently repurposing these.
    /// </para>
    /// </summary>
    public static IVoucherHeadRepository NotFound()
    {
        var repository = new Mock<IVoucherHeadRepository>();

        repository
            .Setup(r => r.GetForUpdateAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((TB_VOUCHERSHEAD?)null);

        return repository.Object;
    }

    /// <summary>A head repository that returns one voucher in the given state.</summary>
    public static IVoucherHeadRepository InState(Guid headId, DocLife? docLife)
    {
        var repository = new Mock<IVoucherHeadRepository>();

        repository
            .Setup(r => r.GetForUpdateAsync(headId, It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new TB_VOUCHERSHEAD { ID = headId, DOCLIFE = docLife });

        return repository.Object;
    }
}
