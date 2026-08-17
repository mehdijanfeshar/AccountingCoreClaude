using Accounting.Domain.Entities;
using Accounting.Domain.Exceptions;
using Accounting.Domain.Tests.TestSupport;
using Accounting.Domain.ValueObjects;
using Xunit;

namespace Accounting.Domain.Tests.Entities;

/// <summary>
/// پوشش بندهای ۱ تا ۵: اعتبارسنجی تفصیلی الزامی/غیرمجاز/تکراری در لحظهٔ Voucher.AddLine
/// (مسیر الف طبق کامنت VoucherLine.AttachDetailValues).
/// </summary>
public class VoucherLineDetailValidationTests
{
    private static Voucher NewDraftVoucher() => Voucher.CreateDraft("1001", DateTime.Today);

    [Fact]
    public void AddLine_WithMissingRequiredDetail_ThrowsRequiredDetailValueMissingException()
    {
        var (_, _, sub) = DomainFactory.CreateHierarchy();
        var customerType = DomainFactory.CreateDetailType("مشتریان");
        sub.LinkDetailType(customerType, DetailRequirement.Required);

        var policy = sub.GetDetailPolicy();
        var voucher = NewDraftVoucher();

        var ex = Assert.Throws<RequiredDetailValueMissingException>(
            () => voucher.AddLine(policy, Money.Of(1000), Money.Zero, detailAccounts: null));

        Assert.Equal(sub.Id, ex.SubsidiaryAccountId);
        Assert.Contains(customerType.Id, ex.MissingDetailAccountTypeIds);
    }

    [Fact]
    public void AddLine_WithRequiredDetailProvided_SucceedsAndAttachesValue()
    {
        var (_, _, sub) = DomainFactory.CreateHierarchy();
        var customerType = DomainFactory.CreateDetailType("مشتریان");
        sub.LinkDetailType(customerType, DetailRequirement.Required);
        var customer = DomainFactory.CreateDetailAccount(customerType, "C-001", "شرکت پارس");

        var policy = sub.GetDetailPolicy();
        var voucher = NewDraftVoucher();

        var line = voucher.AddLine(policy, Money.Of(1000), Money.Zero, new[] { customer });

        Assert.Single(line.DetailValues);
        Assert.Single(voucher.Lines);
        var value = line.DetailValues.Single();
        Assert.Equal(customerType.Id, value.DetailAccountTypeId);
        Assert.Equal(customer.Id, value.DetailAccountId);
    }

    [Fact]
    public void AddLine_WithDetailTypeNotLinked_ThrowsDetailValueNotAllowedException()
    {
        // معینی که هیچ نوع تفصیلی به آن لینک نشده است.
        var (_, _, sub) = DomainFactory.CreateHierarchy();
        var policy = sub.GetDetailPolicy();

        // یک نوع تفصیلی که اصلاً به این معین لینک نشده (= غیرمجاز، طبق قانون ۳).
        var unlinkedType = DomainFactory.CreateDetailType("مراکز هزینه");
        var costCenter = DomainFactory.CreateDetailAccount(unlinkedType, "CC-01", "مرکز هزینهٔ فروش");

        var voucher = NewDraftVoucher();

        var ex = Assert.Throws<DetailValueNotAllowedException>(
            () => voucher.AddLine(policy, Money.Of(500), Money.Zero, new[] { costCenter }));

        Assert.Equal(sub.Id, ex.SubsidiaryAccountId);
        Assert.Equal(unlinkedType.Id, ex.DetailAccountTypeId);
    }

    [Fact]
    public void AddLine_WithTwoDetailValuesOfSameType_ThrowsDuplicateDetailValueException()
    {
        var (_, _, sub) = DomainFactory.CreateHierarchy();
        var customerType = DomainFactory.CreateDetailType("مشتریان");
        sub.LinkDetailType(customerType, DetailRequirement.Optional);

        var customer1 = DomainFactory.CreateDetailAccount(customerType, "C-001", "شرکت پارس");
        var customer2 = DomainFactory.CreateDetailAccount(customerType, "C-002", "شرکت البرز");

        var policy = sub.GetDetailPolicy();
        var voucher = NewDraftVoucher();

        var ex = Assert.Throws<DuplicateDetailValueException>(
            () => voucher.AddLine(policy, Money.Of(500), Money.Zero, new[] { customer1, customer2 }));

        Assert.Equal(sub.Id, ex.SubsidiaryAccountId);
        Assert.Equal(customerType.Id, ex.DetailAccountTypeId);
    }

    /// <summary>
    /// پوشش بند ۵ (تطابق نوع تفصیلی): در طراحی فعلی، VoucherLine نوع تفصیلی را مستقیماً از
    /// DetailAccount.DetailAccountTypeId می‌خواند (رجوع به VoucherLine.AttachDetailValues، خط
    /// «نوع تفصیلی مستقیماً از خودِ DetailAccount خوانده می‌شود»)؛ در نتیجه هیچ پارامتر جداگانه‌ای
    /// برای «نوع تفصیلی موردنظر» وجود ندارد که بتوان آن را با DetailAccount ناهم‌خوان کرد — چنین
    /// حالتی از اساس غیرقابل‌ساخت است (نقطهٔ قوت طراحی: غیرممکن‌سازی ساختاری به‌جای اعتبارسنجی
    /// در زمان اجرا). تجلی عملی این قانون همان چیزی است که در تست بالا
    /// (AddLine_WithDetailTypeNotLinked_ThrowsDetailValueNotAllowedException) پوشش داده شده:
    /// یک DetailAccount که به نوعی تعلق دارد که به این معین لینک نشده، رد می‌شود.
    /// </summary>
    [Fact]
    public void DetailAccount_TypeIsAlwaysReadFromDetailAccountItself_NotFromASeparateParameter()
    {
        var customerType = DomainFactory.CreateDetailType("مشتریان");
        var customer = DomainFactory.CreateDetailAccount(customerType, "C-001", "شرکت پارس");

        Assert.Equal(customerType.Id, customer.DetailAccountTypeId);
    }
}
