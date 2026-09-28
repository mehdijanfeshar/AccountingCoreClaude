using Accounting.Application.Common.Exceptions;
using Accounting.Application.Common.Interfaces;
using MediatR;

namespace Accounting.Application.PettyCash.Queries.GetPettyCashAttachmentFile;

public sealed class GetPettyCashAttachmentFileQueryHandler
    : IRequestHandler<GetPettyCashAttachmentFileQuery, PettyCashAttachmentFileDto?>
{
    private readonly IPettyCashExpenseDocReadRepository _expenseDocReadRepository;
    private readonly IPettyCashAttachmentReadRepository _attachmentReadRepository;

    public GetPettyCashAttachmentFileQueryHandler(
        IPettyCashExpenseDocReadRepository expenseDocReadRepository,
        IPettyCashAttachmentReadRepository attachmentReadRepository)
    {
        _expenseDocReadRepository = expenseDocReadRepository;
        _attachmentReadRepository = attachmentReadRepository;
    }

    public async Task<PettyCashAttachmentFileDto?> Handle(
        GetPettyCashAttachmentFileQuery request, CancellationToken cancellationToken)
    {
        var expenseDoc = await _expenseDocReadRepository.GetByIdAsync(request.ExpenseDocId, request.VahedCode, cancellationToken);

        if (expenseDoc is null)
        {
            throw new NotFoundException("PettyCashExpenseDoc", request.ExpenseDocId);
        }

        return await _attachmentReadRepository.GetFileAsync(request.ExpenseDocId, request.AttachmentId, cancellationToken);
    }
}
