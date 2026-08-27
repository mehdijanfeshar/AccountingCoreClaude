using FluentValidation;

namespace Accounting.Application.BillLogs.Commands.CreateBillLog;

/// <summary>
/// Surface-level (syntactic) validation only, matching the Fluent mapping constraints in
/// <c>LegacyDbContext</c>.
/// </summary>
public sealed class CreateBillLogCommandValidator : AbstractValidator<CreateBillLogCommand>
{
    public CreateBillLogCommandValidator()
    {
        RuleFor(x => x.LogDesc)
            .MaximumLength(1000);

        RuleFor(x => x.LogDate)
            .MaximumLength(8);

        RuleFor(x => x.VahedCode)
            .NotEmpty()
            .MaximumLength(4);

        RuleFor(x => x.Year)
            .NotEmpty()
            .MaximumLength(4);
    }
}
