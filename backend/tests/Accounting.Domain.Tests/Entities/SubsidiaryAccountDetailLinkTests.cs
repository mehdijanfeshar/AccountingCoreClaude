using Accounting.Domain.Exceptions;
using Accounting.Domain.Tests.TestSupport;
using Accounting.Domain.ValueObjects;
using Xunit;

namespace Accounting.Domain.Tests.Entities;

/// <summary>پوشش بند ۱۱: لینک تکراری یک نوع تفصیلی به یک معین باید رد شود.</summary>
public class SubsidiaryAccountDetailLinkTests
{
    [Fact]
    public void LinkDetailType_SameTypeTwice_ThrowsDuplicateDetailTypeLinkException()
    {
        var (_, _, sub) = DomainFactory.CreateHierarchy();
        var customerType = DomainFactory.CreateDetailType("مشتریان");

        sub.LinkDetailType(customerType, DetailRequirement.Required);

        Assert.Throws<DuplicateDetailTypeLinkException>(
            () => sub.LinkDetailType(customerType, DetailRequirement.Optional));
    }

    [Fact]
    public void LinkDetailType_TwoDifferentTypes_BothSucceedAndAppearInPolicy()
    {
        var (_, _, sub) = DomainFactory.CreateHierarchy();
        var customerType = DomainFactory.CreateDetailType("مشتریان");
        var projectType = DomainFactory.CreateDetailType("پروژه‌ها");

        sub.LinkDetailType(customerType, DetailRequirement.Required);
        sub.LinkDetailType(projectType, DetailRequirement.Optional);

        var policy = sub.GetDetailPolicy();

        Assert.True(policy.IsAllowed(customerType.Id));
        Assert.True(policy.IsRequired(customerType.Id));
        Assert.True(policy.IsAllowed(projectType.Id));
        Assert.False(policy.IsRequired(projectType.Id));
    }

    [Fact]
    public void UnlinkDetailType_MakesTypeNoLongerAllowed()
    {
        var (_, _, sub) = DomainFactory.CreateHierarchy();
        var customerType = DomainFactory.CreateDetailType("مشتریان");
        sub.LinkDetailType(customerType, DetailRequirement.Optional);

        sub.UnlinkDetailType(customerType.Id);

        var policy = sub.GetDetailPolicy();
        Assert.False(policy.IsAllowed(customerType.Id));
    }
}
