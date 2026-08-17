using Accounting.Domain.Entities;
using Accounting.Domain.Exceptions;
using Accounting.Domain.Rules;
using Accounting.Domain.Tests.TestSupport;
using Accounting.Domain.ValueObjects;
using Xunit;

namespace Accounting.Domain.Tests.Rules;

/// <summary>
/// پوشش بند ۱۲: فراخوانی مستقیم VoucherPostingValidator (بدون عبور از Voucher.Post) —
/// همان «آخرین خط دفاعی» که طبق کامنت خودِ کلاس، برای موجودیت‌های بازیابی‌شده از EF Core
/// (که سازندهٔ دامنه را دور می‌زنند) لازم است.
/// </summary>
public class VoucherPostingValidatorTests
{
    private static SubsidiaryAccount NewSubsidiary(string subCode)
        => DomainFactory.CreateHierarchy(subCode: subCode).Sub;

    [Fact]
    public void Validate_UnbalancedVoucher_ThrowsVoucherNotBalancedException_AndDoesNotChangeStatus()
    {
        var debitSub = NewSubsidiary("100");
        var creditSub = NewSubsidiary("200");

        var voucher = Voucher.CreateDraft("3001", DateTime.Today);
        voucher.AddLine(debitSub.GetDetailPolicy(), Money.Of(1000), Money.Zero);
        voucher.AddLine(creditSub.GetDetailPolicy(), Money.Zero, Money.Of(900));

        var validator = new VoucherPostingValidator();

        Assert.Throws<VoucherNotBalancedException>(() => validator.Validate(voucher));
        Assert.Equal(VoucherStatus.Draft, voucher.Status);
    }

    [Fact]
    public void Validate_BalancedVoucher_DoesNotThrow()
    {
        var debitSub = NewSubsidiary("100");
        var creditSub = NewSubsidiary("200");

        var voucher = Voucher.CreateDraft("3002", DateTime.Today);
        voucher.AddLine(debitSub.GetDetailPolicy(), Money.Of(1000), Money.Zero);
        voucher.AddLine(creditSub.GetDetailPolicy(), Money.Zero, Money.Of(1000));

        var validator = new VoucherPostingValidator();

        var exception = Record.Exception(() => validator.Validate(voucher));

        Assert.Null(exception);
        Assert.Equal(VoucherStatus.Draft, voucher.Status);
    }

    [Fact]
    public void Validate_EmptyVoucher_ThrowsEmptyVoucherException()
    {
        var voucher = Voucher.CreateDraft("3003", DateTime.Today);
        var validator = new VoucherPostingValidator();

        Assert.Throws<EmptyVoucherException>(() => validator.Validate(voucher));
    }
}
