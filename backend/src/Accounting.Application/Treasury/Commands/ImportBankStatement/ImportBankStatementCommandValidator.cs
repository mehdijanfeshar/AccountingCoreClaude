using FluentValidation;

namespace Accounting.Application.Treasury.Commands.ImportBankStatement;

public sealed class ImportBankStatementCommandValidator : AbstractValidator<ImportBankStatementCommand>
{
    public ImportBankStatementCommandValidator()
    {
        RuleFor(x => x.StatementId).NotEmpty();
        RuleFor(x => x.Content).NotEmpty().WithMessage("فایل صورت‌حساب خالی است.");
        RuleFor(x => x.VahedCode).NotEmpty().MaximumLength(4);
    }
}
