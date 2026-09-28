using Accounting.Application.Common.Exceptions;
using Accounting.Application.Common.Interfaces;
using Accounting.Application.Common.Security;
using Accounting.Domain.ValueObjects;
using MediatR;

namespace Accounting.Application.PettyCash.Commands.DeletePettyCashAttachment;

public sealed class DeletePettyCashAttachmentCommandHandler : IRequestHandler<DeletePettyCashAttachmentCommand>
{
    private readonly IPettyCashExpenseDocRepository _expenseDocRepository;
    private readonly IPettyCashAttachmentRepository _attachmentRepository;
    private readonly IPettyCashDocEventReadRepository _eventReadRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUser _currentUser;

    public DeletePettyCashAttachmentCommandHandler(
        IPettyCashExpenseDocRepository expenseDocRepository,
        IPettyCashAttachmentRepository attachmentRepository,
        IPettyCashDocEventReadRepository eventReadRepository,
        IUnitOfWork unitOfWork,
        ICurrentUser currentUser)
    {
        _expenseDocRepository = expenseDocRepository;
        _attachmentRepository = attachmentRepository;
        _eventReadRepository = eventReadRepository;
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
    }

    public async Task Handle(DeletePettyCashAttachmentCommand request, CancellationToken cancellationToken)
    {
        var doc = await _expenseDocRepository.GetForUpdateAsync(request.ExpenseDocId, request.VahedCode, cancellationToken);

        if (doc is null || doc.ISDELETED)
        {
            throw new NotFoundException("PettyCashExpenseDoc", request.ExpenseDocId);
        }

        PettyCashDocEditability.EnsureEditable(doc.ID, doc.DOC_STATE);

        if (!string.Equals(_currentUser.UserId, doc.ADDUSERID, StringComparison.Ordinal))
        {
            throw new PettyCashAttachmentOwnerOnlyException(doc.ID);
        }

        // تکمیل بخش ۲ (۲۰۲۶-۰۹-۲۸، «قفل فیلدبه‌فیلد»، صفحهٔ ۸): same attachment-editability gate as Upload.
        if (doc.DOC_STATE == PettyCashDocState.Returned)
        {
            var lastReturn = await _eventReadRepository.GetLastReturnEventAsync(doc.ID, cancellationToken);
            var reasonCodes = PettyCashReturnFieldPolicy.ParseReasonCodes(lastReturn?.ReturnReasons);

            if (!PettyCashReturnFieldPolicy.CanEditAttachments(reasonCodes))
            {
                throw new PettyCashReturnFieldLockedException(doc.ID, new[] { "attachments" });
            }
        }

        var attachment = await _attachmentRepository.GetForUpdateAsync(request.Id, request.VahedCode, cancellationToken);

        // An attachment that exists but belongs to a different document than the route claims is
        // treated the same as "does not exist" — same reasoning as DeletePettyCashFundReviewer.
        if (attachment is null || attachment.EXPENSE_DOC_ID != request.ExpenseDocId)
        {
            throw new NotFoundException("PettyCashAttachment", request.Id);
        }

        if (attachment.ISDELETED)
        {
            return;
        }

        attachment.ISDELETED = true;
        attachment.CHANGEUSERID = _currentUser.UserId;
        attachment.UPDATEDDATE = DateTime.UtcNow;

        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
