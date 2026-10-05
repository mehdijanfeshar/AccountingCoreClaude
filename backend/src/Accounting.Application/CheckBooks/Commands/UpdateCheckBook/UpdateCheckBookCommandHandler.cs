using Accounting.Application.Common.Exceptions;
using Accounting.Application.Common.Interfaces;
using MediatR;

namespace Accounting.Application.CheckBooks.Commands.UpdateCheckBook;

/// <summary>
/// Loads the existing <see cref="Accounting.Domain.Entity.TB_CHECKBOOK"/> row via
/// <see cref="ICheckBookRepository.GetForUpdateAsync"/> (change-tracked), overwrites every
/// writable field from the command, stamps audit columns, and owns the transaction boundary by
/// calling <see cref="IUnitOfWork.SaveChangesAsync"/> exactly once. Throws
/// <see cref="NotFoundException"/> — mapped to 404 by <c>GlobalExceptionHandler</c> — when the
/// row does not exist or is already soft-deleted (<c>ISDELETED</c> is non-nullable <c>bool</c>
/// on this table). <c>CHANGEUSERID</c> is sourced from <see cref="ICurrentUser"/> — never from
/// the request. <c>request.VahedCode</c> is equally non-forgeable: by the time this handler
/// runs, <c>VahedScopeBehavior</c> has already overwritten it with the authenticated caller's
/// own unit code (see <see cref="UpdateCheckBookCommand.VahedCode"/>).
/// </summary>
public sealed class UpdateCheckBookCommandHandler : IRequestHandler<UpdateCheckBookCommand>
{
    private readonly ICheckBookRepository _checkBookRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUser _currentUser;

    public UpdateCheckBookCommandHandler(
        ICheckBookRepository checkBookRepository,
        IUnitOfWork unitOfWork,
        ICurrentUser currentUser)
    {
        _checkBookRepository = checkBookRepository;
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
    }

    public async Task Handle(UpdateCheckBookCommand request, CancellationToken cancellationToken)
    {
        var entity = await _checkBookRepository.GetForUpdateAsync(request.Id, request.VahedCode, cancellationToken);

        if (entity is null || entity.ISDELETED)
        {
            throw new NotFoundException("CheckBook", request.Id);
        }

        // صوری ↔ واقعی پس از ثبت عوض نمی‌شود (یکی بی‌برگ است و دیگری برگ‌دار).
        if (CheckBookLeaves.IsSori(entity.CHECKBOOK_TYPE) != CheckBookLeaves.IsSori(request.CheckBookType))
            throw new ChequeConflictException("نوع دسته‌چک (صوری/واقعی) پس از ثبت قابل تغییر نیست؛ دسته‌چک را حذف و دوباره تعریف کنید.");

        var sori = CheckBookLeaves.IsSori(entity.CHECKBOOK_TYPE);

        // عین مرجع: پس از ساخت اوراق، بازهٔ شماره تغییر نمی‌کند. بازهٔ دسته‌چک صوری را سرور ساخته و دست‌نخورده می‌ماند.
        if (!sori && (entity.FROMCHECKNUMBER != request.FromCheckNumber || entity.TOCHECKNUMBER != request.ToCheckNumber))
        {
            var leaves = await _checkBookRepository.GetLeavesForUpdateAsync(entity.ID, cancellationToken);
            if (leaves.Count > 0)
            {
                throw new ChequeConflictException(leaves.Any(CheckBookLeaves.IsUsed)
                    ? "اوراق دسته‌چک انتخابی استفاده شده است و امکان اصلاح از شماره چک و تا شماره چک وجود ندارد."
                    : "برای این دسته‌چک اوراق چک ساخته شده است؛ برای اصلاح بازهٔ شماره، دسته‌چک را حذف و دوباره با شماره‌های درست تعریف کنید.");
            }
        }

        entity.ACCOUNT_ID = request.AccountId;
        entity.CHECKBOOK_TITLE = request.CheckBookTitle;
        entity.CHECKBOOK_DATE = request.CheckBookDate;
        if (!sori)
        {
            entity.FROMCHECKNUMBER = request.FromCheckNumber;
            entity.TOCHECKNUMBER = request.ToCheckNumber;
        }
        entity.CHECKTYPE_ID = request.CheckTypeId;
        entity.VAHEDCODE = request.VahedCode;
        entity.CHECKBOOK_TYPE = request.CheckBookType;
        entity.SERIAL = request.Serial;
        entity.CHANGEUSERID = _currentUser.UserId;
        entity.UPDATEDDATE = DateTime.UtcNow;

        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
