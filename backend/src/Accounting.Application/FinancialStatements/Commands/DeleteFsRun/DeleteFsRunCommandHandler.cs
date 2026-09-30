using Accounting.Application.Common.Exceptions;
using Accounting.Application.Common.Interfaces;
using MediatR;

namespace Accounting.Application.FinancialStatements.Commands.DeleteFsRun;

public sealed class DeleteFsRunCommandHandler : IRequestHandler<DeleteFsRunCommand>
{
    private readonly IFsRunRepository _runRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUser _currentUser;

    public DeleteFsRunCommandHandler(IFsRunRepository runRepository, IUnitOfWork unitOfWork, ICurrentUser currentUser)
    {
        _runRepository = runRepository;
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
    }

    public async Task Handle(DeleteFsRunCommand request, CancellationToken cancellationToken)
    {
        var run = await _runRepository.GetForUpdateAsync(request.Id, request.VahedCode, cancellationToken)
            ?? throw new NotFoundException("FsRun", request.Id);

        // بخش ۴۵-ه — اجرای در گردش تأیید یا منتشرشده سابقهٔ رسمی است و حذف نمی‌شود.
        if (run.STATE is not (Domain.ValueObjects.FsRunState.Draft or Domain.ValueObjects.FsRunState.Superseded))
        {
            throw new FsTemplateConflictException("فقط اجرای پیش‌نویس یا جایگزین‌شده حذف می‌شود؛ اجرای ارسال‌شده، تأییدشده یا منتشرشده سابقهٔ رسمی است.");
        }

        run.ISDELETED = true;
        run.CHANGEUSERID = _currentUser.UserId;
        run.UPDATEDDATE = DateTime.UtcNow;

        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
