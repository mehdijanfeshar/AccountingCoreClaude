using Accounting.Domain.ValueObjects;
using MediatR;

namespace Accounting.Application.Accounts.Commands.CreateAccountCode;

/// <summary>
/// Creates a new <c>TB_ACCOUNTCODE</c> row (Legacy chart-of-accounts coding node). Carries
/// primitive fields only — the handler is responsible for constructing the Domain entity.
/// Returns the newly generated <see cref="Guid"/> ID.
/// </summary>
/// <param name="TypeCode">TYPECODE column — coding level, see <see cref="TypeCodes"/>.</param>
/// <param name="ParentId">Optional parent node in the self-referencing coding hierarchy.</param>
/// <param name="AccCode">Account code (max 6 chars, unique — enforced by DB constraint <c>UK_ACCOUNTCODE</c>).</param>
/// <param name="AccCodeName">Account code title (max 200 chars).</param>
/// <param name="TypeActivity">TYPEACTIVITY column — debit/credit nature, see <see cref="TypeActivity"/>.</param>
/// <param name="SourceAndConsumeId">Optional link to source/consume classification.</param>
/// <param name="IdentyGroupsId">Optional link to identity group.</param>
/// <param name="TypeAccCode">TYPEACCCODE column — temporary/permanent, see <see cref="TypeAccCode"/>.</param>
/// <param name="MoInforClose">MOINFORCLOSE column (max 6 chars).</param>
/// <param name="TypeAction">TYPEACTION column — off-nature posting behavior, see <see cref="TypeAction"/>.</param>
public sealed record CreateAccountCodeCommand(
    TypeCodes? TypeCode,
    Guid? ParentId,
    string AccCode,
    string AccCodeName,
    TypeActivity? TypeActivity,
    Guid? SourceAndConsumeId,
    Guid? IdentyGroupsId,
    TypeAccCode? TypeAccCode,
    string? MoInforClose,
    TypeAction? TypeAction) : IRequest<Guid>;
