using FluentValidation;

namespace Accounting.Application.CheckBooks.Commands.UpdateCheckBook;

/// <summary>
/// Surface-level (syntactic) validation only, matching the Fluent mapping constraints in
/// <c>LegacyDbContext</c>. Per the recorded "Legacy fully replaces the rich model" architecture
/// decision, accounting invariants must NOT be re-created here.
///
/// ✅ <b>These rules really do run now — fixed in phase 31; they did not before.</b>
/// <c>ValidationBehavior</c> used to declare <c>where TRequest : IRequest&lt;TResponse&gt;</c>,
/// which MediatR 14 never satisfies for a void (<c>: IRequest</c>) command, so the DI container
/// skipped the validator for every <c>Update</c>/<c>Delete</c> request project-wide — silently,
/// from phase 8 to phase 30. The constraint is gone and
/// <c>BehaviorPipelineConstraintTests</c> fails if it ever comes back. Practical consequence:
/// rules here were written and unit-tested but never exercised against real traffic, so a 400
/// that appears for the first time is most likely this validator finally firing, not a new bug.
/// </summary>
public sealed class UpdateCheckBookCommandValidator : AbstractValidator<UpdateCheckBookCommand>
{
    public UpdateCheckBookCommandValidator()
    {
        // چک صوری: بازه را سرور می‌سازد (سال + کد واحد + ۰۰۰۱..۱۰۰۰)؛ قواعد بازه فقط برای چک واقعی.
        When(x => !CheckBookLeaves.IsSori(x.CheckBookType), () =>
        {
            CheckBookLeaves.RangeRules(this, x => x.FromCheckNumber, x => x.ToCheckNumber);
            RuleFor(x => x.FromCheckNumber).NotEmpty();
            RuleFor(x => x.ToCheckNumber).NotEmpty();
        });
        RuleFor(x => x.CheckBookDate).Matches("^[0-9]{8}$")
            .WithMessage("تاریخ صدور دسته‌چک صوری الزامی است (سال آن در شمارهٔ چک می‌آید).")
            .When(x => CheckBookLeaves.IsSori(x.CheckBookType));
        RuleFor(x => x.Id)
            .NotEmpty();

        RuleFor(x => x.AccountId)
            .NotEqual(Guid.Empty);

        RuleFor(x => x.CheckBookTitle)
            .MaximumLength(100);

        RuleFor(x => x.CheckBookDate)
            .NotEmpty()
            .MaximumLength(8);

        RuleFor(x => x.FromCheckNumber)
            .MaximumLength(14);

        RuleFor(x => x.ToCheckNumber)
            .MaximumLength(14);

        RuleFor(x => x.VahedCode)
            .NotEmpty()
            .MaximumLength(4);

        RuleFor(x => x.Serial)
            .MaximumLength(20);

        // .IsInEnum() only rejects an out-of-range underlying integer (e.g. (CheckType)99) —
        // added in phase 27 batch 2 alongside the bool?-to-enum fix for this column.
        RuleFor(x => x.CheckBookType)
            .IsInEnum()
            .When(x => x.CheckBookType.HasValue);
    }
}
