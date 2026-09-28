using System.Text.Json.Serialization;
using Accounting.Application.Common.Security;
using Accounting.Application.PettyCash.Commands.Common;
using MediatR;

namespace Accounting.Application.PettyCash.Commands.UpsertPettyCashFundTafsilis;

/// <summary>
/// <c>POST api/petty-cash/funds/{fundId}/tafsilis</c> — full replacement of the fund's
/// <c>TB_PC_FUND_LINK_TAFSILI</c> set (بخش ۳-ب، <c>docs/tankhah-khazaneh-module.md</c> section 9).
/// An empty/null <see cref="Tafsilis"/> soft-deletes every existing row — the fund's حساب معین
/// then has no تفصیلی, which is valid as long as <c>IVoucherTafsiliLevelGuard</c> agrees the
/// معین requires none.
///
/// Deliberately named without "LinkTafsili"/"TafsiliLink" so
/// <c>NoIndependentLinkTableWritePathTests.NoMediatRRequestType_...</c> does not (and should not)
/// flag it — <c>TB_PC_FUND_LINK_TAFSILI</c> stays permanently embedded; this command is the
/// PARENT (<c>TB_PC_FUND</c>) aggregate's own write, not an independent link-table CRUD.
/// </summary>
public sealed record UpsertPettyCashFundTafsilisCommand(
    Guid FundId,
    IReadOnlyList<PettyCashFundTafsiliLinkInput>? Tafsilis) : IRequest, IVahedScopedCommand
{
    [JsonIgnore]
    public string VahedCode { get; set; } = string.Empty;
}
