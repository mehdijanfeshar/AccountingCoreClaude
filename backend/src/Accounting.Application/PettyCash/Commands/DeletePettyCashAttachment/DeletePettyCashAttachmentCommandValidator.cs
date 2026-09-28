using FluentValidation;

namespace Accounting.Application.PettyCash.Commands.DeletePettyCashAttachment;

public sealed class DeletePettyCashAttachmentCommandValidator : AbstractValidator<DeletePettyCashAttachmentCommand>
{
    public DeletePettyCashAttachmentCommandValidator()
    {
        RuleFor(x => x.ExpenseDocId).NotEmpty();
        RuleFor(x => x.Id).NotEmpty();
    }
}
