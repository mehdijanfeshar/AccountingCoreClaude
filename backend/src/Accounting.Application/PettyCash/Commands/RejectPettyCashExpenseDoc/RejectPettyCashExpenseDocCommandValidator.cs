using FluentValidation;

namespace Accounting.Application.PettyCash.Commands.RejectPettyCashExpenseDoc;

public sealed class RejectPettyCashExpenseDocCommandValidator : AbstractValidator<RejectPettyCashExpenseDocCommand>
{
    public RejectPettyCashExpenseDocCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();

        // صفحهٔ ۷ پاورپوینت: «رد نیازمند دلیل است».
        RuleFor(x => x.Note)
            .NotEmpty()
            .WithMessage("دلیل رد الزامی است.")
            .MaximumLength(1000);
    }
}
