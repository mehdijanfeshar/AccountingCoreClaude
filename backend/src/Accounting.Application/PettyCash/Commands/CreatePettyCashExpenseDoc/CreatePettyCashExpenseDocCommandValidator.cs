using FluentValidation;

namespace Accounting.Application.PettyCash.Commands.CreatePettyCashExpenseDoc;

/// <summary>
/// Surface-level validation, matching the Fluent mapping constraints in <c>LegacyDbContext</c>
/// plus the always-applicable §4 amount rule ("ارزش افزوده ≥ ۰ و ≤ مبلغ قبل از مالیات" —
/// <c>docs/tankhah-khazaneh-module.md</c>). The invoice-year/limit/balance rules are NOT here —
/// they require repository reads and only apply when <see cref="CreatePettyCashExpenseDocCommand.Submit"/>
/// is true, so they live in <see cref="Common.IPettyCashSubmitRuleChecker"/> instead (Domain/
/// Application business rules, not syntactic ones — see the project's Validation conventions).
/// </summary>
public sealed class CreatePettyCashExpenseDocCommandValidator : AbstractValidator<CreatePettyCashExpenseDocCommand>
{
    public CreatePettyCashExpenseDocCommandValidator()
    {
        RuleFor(x => x.FundId).NotEmpty();
        RuleFor(x => x.Year).NotEmpty().Length(4);
        RuleFor(x => x.ExpenseId).NotEmpty();

        RuleFor(x => x.RegisterDate)
            .NotEmpty()
            .Length(8);

        RuleFor(x => x.VendorName)
            .NotEmpty()
            .MaximumLength(200);

        RuleFor(x => x.VendorNationalId).MaximumLength(11);
        RuleFor(x => x.InvoiceNo).MaximumLength(50);
        RuleFor(x => x.InvoiceDate).Length(8).When(x => !string.IsNullOrEmpty(x.InvoiceDate));

        // Required only when actually submitting — the invoice-year rule (§4) has nothing to
        // check for a پیش‌نویس that has not been submitted yet.
        RuleFor(x => x.InvoiceDate)
            .NotEmpty()
            .WithMessage("'InvoiceDate' must not be empty when submitting.")
            .When(x => x.Submit);

        RuleFor(x => x.EvidenceType).IsInEnum().When(x => x.EvidenceType.HasValue);

        RuleFor(x => x.Description).MaximumLength(200);

        RuleFor(x => x.AmountBeforeTax).GreaterThanOrEqualTo(0);

        RuleFor(x => x.VatAmount)
            .GreaterThanOrEqualTo(0)
            .Must((command, vat) => vat <= command.AmountBeforeTax)
            .WithMessage("'VatAmount' must not exceed 'AmountBeforeTax'.");
    }
}
