using Accounting.Application.Common.Exceptions;
using Accounting.Application.Common.Security;
using Accounting.Domain.ValueObjects;

namespace Accounting.Application.Tests.Common.Security;

/// <summary>
/// The phase-38 rule (project owner, 2026-09-22): a voucher may be edited or deleted only while it
/// is یادداشت or موقت. Reviewed/accepted vouchers are view-only and must be moved back with the
/// explicit change-state operation first.
/// </summary>
public sealed class VoucherEditabilityTests
{
    [Theory]
    [InlineData(DocLife.Draft)]
    [InlineData(DocLife.Temporary)]
    public void Draft_and_temporary_are_editable(DocLife docLife)
    {
        Assert.True(VoucherEditability.IsEditable(docLife));

        // Must not throw.
        VoucherEditability.EnsureEditable(Guid.NewGuid(), docLife);
    }

    [Theory]
    [InlineData(DocLife.Reviewed)]
    [InlineData(DocLife.Accepted)]
    public void Reviewed_and_accepted_are_locked(DocLife docLife)
    {
        Assert.False(VoucherEditability.IsEditable(docLife));

        var id = Guid.NewGuid();
        var ex = Assert.Throws<VoucherNotEditableException>(() => VoucherEditability.EnsureEditable(id, docLife));

        Assert.Equal(id, ex.VoucherHeadId);
        Assert.Equal(docLife, ex.DocLife);
    }

    [Fact]
    public void A_null_state_is_not_editable()
    {
        // Fails closed. TB_VOUCHERSHEAD.DOCLIFE has an Oracle DEFAULT of 0, which is outside the
        // enum and has no known meaning — letting "unknown" mean "editable" would make the one
        // value nobody can explain the most permissive one.
        Assert.False(VoucherEditability.IsEditable(null));
        Assert.Throws<VoucherNotEditableException>(() => VoucherEditability.EnsureEditable(Guid.NewGuid(), null));
    }

    [Fact]
    public void A_value_outside_the_enum_is_not_editable()
    {
        var outsideTheEnum = (DocLife)0;

        Assert.False(VoucherEditability.IsEditable(outsideTheEnum));
        Assert.Throws<VoucherNotEditableException>(
            () => VoucherEditability.EnsureEditable(Guid.NewGuid(), outsideTheEnum));
    }

    [Fact]
    public void The_message_names_the_state_and_the_way_out()
    {
        var ex = new VoucherNotEditableException(Guid.NewGuid(), DocLife.Accepted);

        // Without the second half the error is a dead end: the accountant needs to know that
        // moving the voucher back is what unblocks them.
        Assert.Contains("تأیید دائم", ex.PublicDetail);
        Assert.Contains("یادداشت", ex.PublicDetail);
        Assert.Contains("موقت", ex.PublicDetail);
    }

    [Fact]
    public void The_public_message_never_leaks_the_voucher_id()
    {
        var id = Guid.NewGuid();
        var ex = new VoucherNotEditableException(id, DocLife.Reviewed);

        // The id belongs in the log (via Message), not in the response body.
        Assert.DoesNotContain(id.ToString(), ex.PublicDetail);
        Assert.Contains(id.ToString(), ex.Message);
    }
}
