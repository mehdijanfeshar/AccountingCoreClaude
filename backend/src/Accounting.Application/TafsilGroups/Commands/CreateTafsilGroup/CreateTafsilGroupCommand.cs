using Accounting.Domain.ValueObjects;
using MediatR;

namespace Accounting.Application.TafsilGroups.Commands.CreateTafsilGroup;

/// <summary>
/// Creates a new <c>TB_TAFSIL_GROUP</c> row (Legacy tafsili-group lookup). Carries primitive
/// fields only — the handler is responsible for constructing the Domain entity. Returns the
/// newly generated <see cref="Guid"/> ID.
///
/// Uniqueness: the combination <c>(TafsilGroupCode, ISDELETED)</c> is enforced by the Oracle
/// constraint <c>UK_TBTAFSILGROUP</c>. No pre-check is performed here — a duplicate combination
/// surfaces as an Oracle ORA-00001, translated centrally by
/// <c>UnitOfWork.SaveChangesAsync</c> into a
/// <see cref="Accounting.Application.Common.Exceptions.DuplicateKeyException"/> → 409.
///
/// ⚠️ Observation (not enforced here): because <c>ISDELETED</c> itself participates in the
/// unique key, soft-deleting a row frees its <see cref="TafsilGroupCode"/> for reuse only once —
/// a second soft-deleted row with the same code would still collide with the first, since both
/// would carry <c>ISDELETED = true</c>. This is a pre-existing constraint characteristic, not
/// something this command attempts to work around.
/// </summary>
/// <param name="TafsilGroupCode">TAFSILGROUP_CODE column (max 3 chars, required — participates in <c>UK_TBTAFSILGROUP</c>).</param>
/// <param name="TafsilGroupName">TAFSILGROUP_NAME column (max 200 chars, required).</param>
/// <param name="PersonType">
/// PERSONTYPE column — <see cref="PersonTypes"/> (1=Person, 2=Legal, 3=Other). Resolved from the
/// project-wide <c>bool?</c>/enum scaffolding bug documented in CLAUDE.md risk #2 in phase 27
/// batch 1 — see <c>docs/centralaccount-business-reference.md</c> §24-1.
/// </param>
public sealed record CreateTafsilGroupCommand(
    string TafsilGroupCode,
    string TafsilGroupName,
    PersonTypes? PersonType) : IRequest<Guid>;
