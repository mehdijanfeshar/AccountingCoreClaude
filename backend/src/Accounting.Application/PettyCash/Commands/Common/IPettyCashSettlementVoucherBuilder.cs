using Accounting.Application.PettyCash.Queries;
using Accounting.Domain.Entity;

namespace Accounting.Application.PettyCash.Commands.Common;

/// <summary>
/// Builds and stages (never saves) the تسویهٔ دوره GL voucher — بخش ۳-ب
/// (<c>docs/tankhah-khazaneh-module.md</c> section 9). This is the ONLY place a صورت تسویه
/// becomes a <see cref="TB_VOUCHERSHEAD"/>/<see cref="TB_VOUCHERSDETAIL"/> pair, so
/// <c>FinalizePettyCashSettlementCommandHandler</c> never constructs voucher entities itself.
///
/// Deliberately built entirely on top of the SAME repositories/guard every other voucher write
/// path uses — <see cref="Accounting.Application.Common.Interfaces.IVoucherHeadRepository"/>,
/// <see cref="Accounting.Application.Common.Interfaces.IVoucherDetailRepository"/> and
/// <see cref="Accounting.Application.Vouchers.Commands.Common.IVoucherTafsiliLevelGuard"/> — the
/// exact same three components <c>CreateVoucherHeadCommandHandler</c>/
/// <c>CreateVoucherDetailCommandHandler</c>/<c>ReverseVoucherCommandHandler</c> use. Nothing here
/// re-implements "how a voucher head/line/تفصیلی-link gets written" in parallel; it only supplies
/// the settlement's own shape (one debit line per مادهٔ هزینه group, one credit line for the
/// fund's حساب معین) built from those shared building blocks.
/// </summary>
public interface IPettyCashSettlementVoucherBuilder
{
    /// <exception cref="Accounting.Application.Common.Exceptions.PettyCashSettlementFundAccountMissingException">
    /// <c>fund.ACCOUNTCODE_ID</c> is null.</exception>
    /// <exception cref="Accounting.Application.Common.Exceptions.PettyCashSettlementExpenseAccountMissingException">
    /// One of <paramref name="expenseGroups"/> has no <c>AccountCodeId</c>.</exception>
    /// <exception cref="Accounting.Application.Common.Exceptions.PettyCashSettlementTafsiliMissingException">
    /// A line fails «تفصیلی الزامی» (<see cref="Accounting.Application.Vouchers.Commands.Common.IVoucherTafsiliLevelGuard"/>).</exception>
    /// <exception cref="Accounting.Application.Common.Exceptions.PettyCashSettlementUnbalancedException">
    /// Defensive — see that exception's XML doc.</exception>
    Task<PettyCashSettlementVoucherResult> BuildAndStageAsync(
        TB_PC_FUND fund,
        TB_PC_SETTLEMENT_PERIOD period,
        IReadOnlyList<PettyCashSettlementExpenseGroupDto> expenseGroups,
        string vahedCode,
        CancellationToken cancellationToken = default);
}

public sealed record PettyCashSettlementVoucherResult(Guid VoucherHeadId, string DocNum, decimal TotalAmount);
