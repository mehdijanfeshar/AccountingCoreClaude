using FluentValidation;

namespace Accounting.Application.PettyCash.Commands.CreatePettyCashReplenishment;

/// <summary>Surface-level (syntactic) validation only — the "no documents to replenish"/ceiling
/// checks are Application-level (async, DB-backed) and live in the handler.</summary>
public sealed class CreatePettyCashReplenishmentCommandValidator : AbstractValidator<CreatePettyCashReplenishmentCommand>
{
    public CreatePettyCashReplenishmentCommandValidator()
    {
        RuleFor(x => x.FundId).NotEmpty();
        RuleFor(x => x.SourceBankAccountId).NotEmpty();

        RuleFor(x => x.PaymentMethod).IsInEnum();

        RuleFor(x => x.RegisterDate)
            .NotEmpty()
            .Matches("^[0-9]{8}$").WithMessage("تاریخ ثبت باید به شکل YYYYMMDD باشد.");

        RuleFor(x => x.Year)
            .NotEmpty()
            .Matches("^[0-9]{4}$").WithMessage("سال مالی باید ۴ رقم عددی باشد.");

        RuleFor(x => x.Note).MaximumLength(1000);

        RuleFor(x => x.VahedCode)
            .NotEmpty()
            .MaximumLength(4);
    }
}
