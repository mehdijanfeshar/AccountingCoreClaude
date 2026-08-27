using FluentValidation;

namespace Accounting.Application.Rabets.Commands.CreateRabet;

/// <summary>
/// Surface-level (syntactic) validation only, matching the Fluent mapping constraints in
/// <c>LegacyDbContext</c>. There are deliberately no rules here: both
/// <see cref="CreateRabetCommand.RabetTypeId"/> and <see cref="CreateRabetCommand.AccountCodeId"/>
/// are optional <see cref="Guid"/> foreign keys with no format constraint beyond what the DB
/// (UNIQUE <c>UK_RABET</c>, and the two FK constraints) already enforces — inventing a
/// NotEmpty/NotEqual(Guid.Empty) rule here would be a business rule this project has not been
/// asked to add. Per the recorded "Legacy fully replaces the rich model" architecture decision,
/// accounting invariants must NOT be re-created here.
/// </summary>
public sealed class CreateRabetCommandValidator : AbstractValidator<CreateRabetCommand>
{
    public CreateRabetCommandValidator()
    {
    }
}
