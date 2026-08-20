using MediatR;

namespace Accounting.Application.Accounts.Commands.UpdateAccountCode;

/// <summary>
/// Fully replaces the writable fields of an existing <c>TB_ACCOUNTCODE</c> row (PUT
/// semantics, not PATCH). This is deliberate: almost every Legacy column here is nullable
/// (<c>bool?</c>, <c>Guid?</c>, <c>string?</c>), so a partial-update model cannot distinguish
/// "field omitted by the caller" from "field explicitly set to null" without wrapping every
/// field in an <c>Optional&lt;T&gt;</c>-style marker — which both breaks the "commands carry
/// only primitives" rule and adds machinery disproportionate to the need. PUT also keeps this
/// command symmetric with <c>CreateAccountCodeCommand</c>.
///
/// Deliberately excludes <c>ID</c>, <c>ADDUSERID</c>, <c>CREATEDDATE</c> and <c>ISDELETED</c>:
/// identity and creation audit are immutable after insert, and <c>ISDELETED</c> is owned
/// exclusively by <c>DeleteAccountCodeCommand</c> — allowing it here would turn Update into a
/// back door for delete/undelete. <c>CHANGEUSERID</c>/<c>UPDATEDDATE</c> are likewise absent
/// because the handler sources them from <see cref="Accounting.Application.Common.Interfaces.ICurrentUser"/>
/// and the server clock, never from client input.
/// </summary>
/// <param name="Id">The <c>TB_ACCOUNTCODE.ID</c> to update (bound from the route, never the body).</param>
/// <param name="TypeCode">TYPECODE column (legacy boolean flag).</param>
/// <param name="ParentId">Optional parent node in the self-referencing coding hierarchy.</param>
/// <param name="AccCode">Account code (max 6 chars, unique — enforced by DB constraint <c>UK_ACCOUNTCODE</c>).</param>
/// <param name="AccCodeName">Account code title (max 200 chars).</param>
/// <param name="TypeActivity">TYPEACTIVITY column — نوع فعالیت (بستانکار/بدهکار/بد-بس).</param>
/// <param name="SourceAndConsumeId">Optional link to source/consume classification.</param>
/// <param name="IdentyGroupsId">Optional link to identity group.</param>
/// <param name="TypeAccCode">TYPEACCCODE column — نوع حساب (موقت/دائم).</param>
/// <param name="MoInforClose">MOINFORCLOSE column (max 6 chars).</param>
/// <param name="TypeAction">TYPEACTION column — نوع خلاف ماهیت.</param>
public sealed record UpdateAccountCodeCommand(
    Guid Id,
    bool? TypeCode,
    Guid? ParentId,
    string AccCode,
    string AccCodeName,
    bool? TypeActivity,
    Guid? SourceAndConsumeId,
    Guid? IdentyGroupsId,
    bool? TypeAccCode,
    string? MoInforClose,
    bool? TypeAction) : IRequest;
