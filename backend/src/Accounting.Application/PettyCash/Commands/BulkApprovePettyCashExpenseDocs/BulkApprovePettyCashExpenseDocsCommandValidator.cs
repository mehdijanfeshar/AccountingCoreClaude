using FluentValidation;

namespace Accounting.Application.PettyCash.Commands.BulkApprovePettyCashExpenseDocs;

public sealed class BulkApprovePettyCashExpenseDocsCommandValidator : AbstractValidator<BulkApprovePettyCashExpenseDocsCommand>
{
    public BulkApprovePettyCashExpenseDocsCommandValidator()
    {
        RuleFor(x => x.Ids)
            .NotEmpty()
            .WithMessage("حداقل یک شناسهٔ سند باید ارسال شود.");

        RuleForEach(x => x.Ids).NotEmpty();
    }
}
