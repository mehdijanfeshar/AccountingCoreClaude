using Accounting.Domain.Entities;
using Accounting.Domain.Exceptions;
using Accounting.Domain.Tests.TestSupport;
using Accounting.Domain.ValueObjects;
using Xunit;

namespace Accounting.Domain.Tests.Entities;

/// <summary>پوشش بندهای ۷، ۸ و ۹: تراز/عدم‌تراز سند، سند خالی، و تغییرناپذیری پس از Post.</summary>
public class VoucherPostingTests
{
    private static SubsidiaryAccount NewSubsidiary(string subCode = "100")
        => DomainFactory.CreateHierarchy(subCode: subCode).Sub;

    [Fact]
    public void Post_UnbalancedVoucher_ThrowsVoucherNotBalancedException()
    {
        var debitSub = NewSubsidiary("100");
        var creditSub = NewSubsidiary("200");

        var voucher = Voucher.CreateDraft("2001", DateTime.Today);
        voucher.AddLine(debitSub.GetDetailPolicy(), Money.Of(1000), Money.Zero);
        voucher.AddLine(creditSub.GetDetailPolicy(), Money.Zero, Money.Of(700));

        var ex = Assert.Throws<VoucherNotBalancedException>(() => voucher.Post());

        Assert.Equal(1000m, ex.TotalDebit);
        Assert.Equal(700m, ex.TotalCredit);
        Assert.Equal(VoucherStatus.Draft, voucher.Status);
    }

    [Fact]
    public void Post_BalancedVoucher_SucceedsAndSetsStatusToPosted()
    {
        var debitSub = NewSubsidiary("100");
        var creditSub = NewSubsidiary("200");

        var voucher = Voucher.CreateDraft("2002", DateTime.Today);
        voucher.AddLine(debitSub.GetDetailPolicy(), Money.Of(1000), Money.Zero);
        voucher.AddLine(creditSub.GetDetailPolicy(), Money.Zero, Money.Of(1000));

        voucher.Post();

        Assert.Equal(VoucherStatus.Posted, voucher.Status);
        Assert.NotNull(voucher.PostedAtUtc);
        Assert.True(voucher.IsBalanced);
    }

    [Fact]
    public void Post_VoucherWithNoLines_ThrowsEmptyVoucherException()
    {
        var voucher = Voucher.CreateDraft("2003", DateTime.Today);

        Assert.Throws<EmptyVoucherException>(() => voucher.Post());
    }

    [Fact]
    public void AddLine_AfterPost_ThrowsVoucherImmutableException()
    {
        var debitSub = NewSubsidiary("100");
        var creditSub = NewSubsidiary("200");

        var voucher = Voucher.CreateDraft("2004", DateTime.Today);
        voucher.AddLine(debitSub.GetDetailPolicy(), Money.Of(500), Money.Zero);
        voucher.AddLine(creditSub.GetDetailPolicy(), Money.Zero, Money.Of(500));
        voucher.Post();

        Assert.Throws<VoucherImmutableException>(
            () => voucher.AddLine(debitSub.GetDetailPolicy(), Money.Of(1), Money.Zero));
    }

    [Fact]
    public void RemoveLine_AfterPost_ThrowsVoucherImmutableException()
    {
        var debitSub = NewSubsidiary("100");
        var creditSub = NewSubsidiary("200");

        var voucher = Voucher.CreateDraft("2005", DateTime.Today);
        var line = voucher.AddLine(debitSub.GetDetailPolicy(), Money.Of(500), Money.Zero);
        voucher.AddLine(creditSub.GetDetailPolicy(), Money.Zero, Money.Of(500));
        voucher.Post();

        Assert.Throws<VoucherImmutableException>(() => voucher.RemoveLine(line));
    }

    [Fact]
    public void Post_AfterAlreadyPosted_ThrowsVoucherImmutableException()
    {
        var debitSub = NewSubsidiary("100");
        var creditSub = NewSubsidiary("200");

        var voucher = Voucher.CreateDraft("2006", DateTime.Today);
        voucher.AddLine(debitSub.GetDetailPolicy(), Money.Of(500), Money.Zero);
        voucher.AddLine(creditSub.GetDetailPolicy(), Money.Zero, Money.Of(500));
        voucher.Post();

        Assert.Throws<VoucherImmutableException>(() => voucher.Post());
    }

    [Fact]
    public void TotalDebitAndTotalCredit_AggregateAcrossAllLines()
    {
        var debitSub = NewSubsidiary("100");
        var creditSub = NewSubsidiary("200");

        var voucher = Voucher.CreateDraft("2007", DateTime.Today);
        voucher.AddLine(debitSub.GetDetailPolicy(), Money.Of(300), Money.Zero);
        voucher.AddLine(debitSub.GetDetailPolicy(), Money.Of(200), Money.Zero);
        voucher.AddLine(creditSub.GetDetailPolicy(), Money.Zero, Money.Of(500));

        Assert.Equal(500m, voucher.TotalDebit.Amount);
        Assert.Equal(500m, voucher.TotalCredit.Amount);
        Assert.True(voucher.IsBalanced);
    }
}
