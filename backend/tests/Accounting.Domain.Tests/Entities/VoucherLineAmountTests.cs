using Accounting.Domain.Entities;
using Accounting.Domain.Exceptions;
using Accounting.Domain.Tests.TestSupport;
using Accounting.Domain.ValueObjects;
using Xunit;

namespace Accounting.Domain.Tests.Entities;

/// <summary>پوشش بند ۶ (بخش قوانین مبلغ ردیف): همزمان بدهکار/بستانکار یا هر دو صفر مجاز نیست.</summary>
public class VoucherLineAmountTests
{
    [Fact]
    public void AddLine_WithBothDebitAndCreditNonZero_ThrowsInvalidVoucherLineAmountException()
    {
        var (_, _, sub) = DomainFactory.CreateHierarchy();
        var policy = sub.GetDetailPolicy();
        var voucher = Voucher.CreateDraft("1002", DateTime.Today);

        Assert.Throws<InvalidVoucherLineAmountException>(
            () => voucher.AddLine(policy, Money.Of(100), Money.Of(50)));
    }

    [Fact]
    public void AddLine_WithBothDebitAndCreditZero_ThrowsInvalidVoucherLineAmountException()
    {
        var (_, _, sub) = DomainFactory.CreateHierarchy();
        var policy = sub.GetDetailPolicy();
        var voucher = Voucher.CreateDraft("1003", DateTime.Today);

        Assert.Throws<InvalidVoucherLineAmountException>(
            () => voucher.AddLine(policy, Money.Zero, Money.Zero));
    }

    [Fact]
    public void AddLine_WithOnlyDebit_Succeeds()
    {
        var (_, _, sub) = DomainFactory.CreateHierarchy();
        var policy = sub.GetDetailPolicy();
        var voucher = Voucher.CreateDraft("1004", DateTime.Today);

        var line = voucher.AddLine(policy, Money.Of(100), Money.Zero);

        Assert.Equal(100m, line.Debit.Amount);
        Assert.Equal(0m, line.Credit.Amount);
    }
}
