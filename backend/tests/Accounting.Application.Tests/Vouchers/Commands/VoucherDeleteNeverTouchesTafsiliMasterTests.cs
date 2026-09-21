using System.Reflection;
using Accounting.Domain.Entity;

namespace Accounting.Application.Tests.Vouchers.Commands;

/// <summary>
/// Pins a rule the project owner stated explicitly (2026-09-21): <b>deleting a voucher, or one of
/// its lines, must never delete anything from <c>TB_TAFSILI</c>.</b>
///
/// <para>
/// The distinction is easy to lose. A voucher line's تفصیلی assignment lives in
/// <c>TB_VOUCHERDETAIL_LINK_TAFSILI</c> — the link row — while <c>TB_TAFSILI</c> is the master
/// record that other vouchers, accounts and reports across the whole unit point at. Cascading from
/// a voucher into the master would delete a shared entity because one document that referenced it
/// went away.
/// </para>
///
/// <para>
/// The cascade is correct today: it soft-deletes the head, its lines, and their link rows, and
/// touches nothing else. This test exists because "correct today" is not the same as "guarded" —
/// the cascade already reaches three tables, and a fourth is a plausible next step for someone who
/// reads "delete the voucher's تفصیلی" as meaning the master.
/// </para>
///
/// <para>
/// It is a type-level assertion rather than a behavioural one on purpose: a behavioural test can
/// only prove the master survived the rows a particular case happened to seed, while this proves
/// the handlers cannot name that entity at all.
/// </para>
///
/// <para>
/// ⚠️ <b>What it does not cover.</b> The handlers delegate to repositories, and a repository could
/// reach <c>TB_TAFSILI</c> through <c>LegacyDbContext</c> without the handler's own types ever
/// mentioning it. That side was checked by reading both cascade repositories: neither names the
/// master table. This test guards the half that reflection can reach.
/// </para>
/// </summary>
public sealed class VoucherDeleteNeverTouchesTafsiliMasterTests
{
    private static readonly string[] DeletePathTypeNames =
    [
        "Accounting.Application.Vouchers.Commands.DeleteVoucherHead.DeleteVoucherHeadCommandHandler",
        "Accounting.Application.Vouchers.Commands.DeleteVoucherDetail.DeleteVoucherDetailCommandHandler",
    ];

    [Fact]
    public void NoVoucherDeleteHandler_ReferencesTheTafsiliMasterEntity()
    {
        var assembly = typeof(Accounting.Application.Common.Security.IVahedScoped).Assembly;

        foreach (var typeName in DeletePathTypeNames)
        {
            var handler = assembly.GetType(typeName);
            Assert.NotNull(handler);

            var mentionsMaster = handler!
                .GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static)
                .SelectMany(method => new[] { method.ReturnType }.Concat(method.GetParameters().Select(p => p.ParameterType)))
                .Concat(handler.GetFields(BindingFlags.NonPublic | BindingFlags.Instance).Select(f => f.FieldType))
                .Any(ReferencesTafsiliMaster);

            Assert.False(
                mentionsMaster,
                $"{typeName} names TB_TAFSILI somewhere in its surface. Deleting a voucher or one " +
                "of its lines must only soft-delete the head, its lines and their " +
                "TB_VOUCHERDETAIL_LINK_TAFSILI rows. TB_TAFSILI is a master record shared across " +
                "the unit — removing it because one voucher referenced it destroys data that other " +
                "vouchers, accounts and reports still point at.");
        }
    }

    /// <summary>
    /// True for <c>TB_TAFSILI</c> itself and for any generic type carrying it (a repository
    /// interface, a list, a task), so a dependency cannot hide inside a type argument.
    /// </summary>
    private static bool ReferencesTafsiliMaster(Type type)
    {
        if (type == typeof(TB_TAFSILI))
        {
            return true;
        }

        return type.IsGenericType && type.GetGenericArguments().Any(ReferencesTafsiliMaster);
    }
}
