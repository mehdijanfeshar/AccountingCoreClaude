using MediatR;

namespace Accounting.Application.Accounts.Commands.CreateAccountCode;

/// <summary>
/// Creates a new <c>TB_ACCOUNTCODE</c> row (Legacy chart-of-accounts coding node). Carries
/// primitive fields only — the handler is responsible for constructing the Domain entity.
/// Returns the newly generated <see cref="Guid"/> ID.
/// </summary>
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
public sealed record CreateAccountCodeCommand(
    bool? TypeCode,
    Guid? ParentId,
    string AccCode,
    string AccCodeName,
    bool? TypeActivity,
    Guid? SourceAndConsumeId,
    Guid? IdentyGroupsId,
    bool? TypeAccCode,
    string? MoInforClose,
    bool? TypeAction) : IRequest<Guid>;
