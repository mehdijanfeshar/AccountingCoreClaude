namespace Accounting.Application.Treasury.Queries;

/// <summary>
/// One row of <c>GET api/treasury/approval-cartable</c> — a merged view over two independent
/// sources (پرداخت درخواست‌های Pending* + تنخواه ترمیم‌های <c>PendingTreasurer</c>), combined in
/// C# after two flat, independent queries — never a cross-module SQL join
/// (<c>docs/tankhah-khazaneh-module.md</c> §۱۰).
/// </summary>
/// <param name="Nature"><c>"payment"</c> or <c>"replenishment"</c> — tells the frontend which
/// detail route/action set to use.</param>
/// <param name="BeneficiaryOrFund">Beneficiary name for a payment request; fund name for a
/// ترمیم.</param>
/// <param name="State">The raw state's underlying int value (different enums per
/// <see cref="Nature"/>) — the frontend never needs to compare it across natures, only display
/// <see cref="StateLabel"/>.</param>
/// <param name="PendingForMe">True when the caller holds the role required by the item's current
/// stage and is not its own creator.</param>
public sealed record ApprovalCartableItemDto(
    string Nature,
    Guid Id,
    string Code,
    string BeneficiaryOrFund,
    string? Description,
    decimal Amount,
    int State,
    string StateLabel,
    int AgeDays,
    string? DueDate,
    bool PendingForMe);
