using Accounting.Application.Common.Interfaces;
using Moq;

namespace Accounting.Application.Tests.Vouchers.Commands.Common;

/// <summary>Detail repository stub that reports every requested voucher as balanced (one line).</summary>
internal static class BalancedDetailRepository
{
    public static IVoucherDetailRepository Create()
    {
        var mock = new Mock<IVoucherDetailRepository>();
        mock.Setup(r => r.GetTotalsByHeadsAsync(It.IsAny<IReadOnlyCollection<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyCollection<Guid> ids, CancellationToken _) =>
                ids.ToDictionary(id => id, _ => new VoucherTotals(1m, 1m, 1)));
        return mock.Object;
    }
}
