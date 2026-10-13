using FluentValidation;

namespace Accounting.Application.Queries.PettyCash.GetPettyCashAttachments;

public sealed class GetPettyCashAttachmentsQueryValidator : AbstractValidator<GetPettyCashAttachmentsQuery>
{
    public GetPettyCashAttachmentsQueryValidator()
    {
        RuleFor(x => x.ExpenseDocId).NotEmpty();
    }
}
