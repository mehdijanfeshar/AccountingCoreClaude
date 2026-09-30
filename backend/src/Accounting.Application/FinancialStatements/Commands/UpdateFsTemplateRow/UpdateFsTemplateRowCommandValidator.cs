using Accounting.Application.FinancialStatements.Commands.Common;
using FluentValidation;

namespace Accounting.Application.FinancialStatements.Commands.UpdateFsTemplateRow;

public sealed class UpdateFsTemplateRowCommandValidator : AbstractValidator<UpdateFsTemplateRowCommand>
{
    public UpdateFsTemplateRowCommandValidator()
    {
        RuleFor(x => x.VersionId).NotEqual(Guid.Empty);
        RuleFor(x => x.RowId).NotEqual(Guid.Empty);
        RuleFor(x => x.Row).NotNull().SetValidator(new FsTemplateRowInputValidator());
    }
}
