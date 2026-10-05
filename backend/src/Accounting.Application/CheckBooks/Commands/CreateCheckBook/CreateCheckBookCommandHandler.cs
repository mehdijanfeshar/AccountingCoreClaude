using Accounting.Application.Common.Exceptions;
using Accounting.Application.Common.Interfaces;
using Accounting.Domain.Entity;
using MediatR;

namespace Accounting.Application.CheckBooks.Commands.CreateCheckBook;

/// <summary>
/// Constructs the <see cref="TB_CHECKBOOK"/> Domain entity from the command, stages it via
/// <see cref="ICheckBookRepository"/>, and owns the transaction boundary by calling
/// <see cref="IUnitOfWork.SaveChangesAsync"/> exactly once. <c>ADDUSERID</c> is sourced from
/// <see cref="ICurrentUser"/> (the authenticated caller) — never from the request — so it cannot
/// be forged by the client. <c>request.VahedCode</c> is equally non-forgeable: by the time this
/// handler runs, <c>VahedScopeBehavior</c> has already overwritten it with the authenticated
/// caller's own unit code (see <see cref="CreateCheckBookCommand.VahedCode"/>), so mapping it
/// onto the entity here is safe.
///
/// <c>ID</c> is always generated application-side (<see cref="Guid.NewGuid"/>); the Oracle
/// column has no <c>sys_guid()</c> default, so this simply follows the project-wide convention.
/// </summary>
public sealed class CreateCheckBookCommandHandler : IRequestHandler<CreateCheckBookCommand, Guid>
{
    private readonly ICheckBookRepository _checkBookRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUser _currentUser;

    public CreateCheckBookCommandHandler(
        ICheckBookRepository checkBookRepository,
        IUnitOfWork unitOfWork,
        ICurrentUser currentUser)
    {
        _checkBookRepository = checkBookRepository;
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
    }

    public async Task<Guid> Handle(CreateCheckBookCommand request, CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;
        var sori = CheckBookLeaves.IsSori(request.CheckBookType);
        var year = request.CheckBookDate[..Math.Min(4, request.CheckBookDate.Length)];

        // چک صوری: بازهٔ سال + کد واحد را سرور می‌سازد؛ بی‌برگ — برگ هنگام استفاده در سند ساخته می‌شود.
        var (from, to) = sori
            ? CheckBookLeaves.SoriRange(year, request.VahedCode)
            : (request.FromCheckNumber, request.ToCheckNumber);

        // UK_CHECKBOOK (حساب + اولین + آخرین + واحد) ردیف‌های حذف‌نرم‌شده را هم می‌شمارد: دسته‌چک فعال هم‌کلید ⇒ ۴۰۹ با
        // پیام روشن؛ دسته‌چک حذف‌شدهٔ هم‌کلید ⇒ همان ردیف با مشخصات تازه بازگردانده می‌شود (درج تکراری ORA-00001 می‌داد).
        var existing = await _checkBookRepository.FindSameRangeForUpdateAsync(
            request.AccountId, from, to, request.VahedCode, cancellationToken);
        if (existing is { ISDELETED: false })
        {
            throw new ChequeConflictException(sori
                ? $"برای این حساب بانکی در سال {year} دسته‌چک صوری تعریف شده است."
                : $"دسته‌چک {from} تا {to} برای این حساب بانکی قبلاً ثبت شده است.");
        }

        var entity = existing ?? new TB_CHECKBOOK { ID = Guid.NewGuid() };
        entity.ACCOUNT_ID = request.AccountId;
        entity.CHECKBOOK_TITLE = request.CheckBookTitle;
        entity.CHECKBOOK_DATE = request.CheckBookDate;
        entity.FROMCHECKNUMBER = from;
        entity.TOCHECKNUMBER = to;
        entity.CHECKTYPE_ID = request.CheckTypeId;
        entity.VAHEDCODE = request.VahedCode;
        entity.CHECKBOOK_TYPE = request.CheckBookType;
        entity.SERIAL = request.Serial;
        entity.ISDELETED = false;

        if (existing is null)
        {
            entity.ADDUSERID = _currentUser.UserId;
            entity.CREATEDDATE = now;
            if (!sori)
            {
                // اوراق چک (ریسک ۲-ج) — عین AddCheckPapers مرجع، در همان Aggregate و همان تراکنش.
                CheckBookLeaves.Generate(entity, Array.Empty<string>(), _currentUser.UserId, now);
            }

            await _checkBookRepository.AddAsync(entity, cancellationToken);
        }
        else
        {
            entity.CHANGEUSERID = _currentUser.UserId;
            entity.UPDATEDDATE = now;

            // اوراق قبلی همان دسته‌چک برمی‌گردند (حذف فقط وقتی مجاز بود که برگی استفاده نشده باشد)؛ برگ جاافتاده ساخته می‌شود.
            var leaves = await _checkBookRepository.GetLeavesForUpdateAsync(entity.ID, cancellationToken, includeDeleted: true);
            foreach (var leaf in leaves.Where(l => l.ISDELETED))
            {
                leaf.ISDELETED = false;
                leaf.UPDATEDDATE = now;
            }

            if (!sori)
                CheckBookLeaves.Generate(entity, leaves.Select(l => l.CHEQ_NO).ToList(), _currentUser.UserId, now);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return entity.ID;
    }
}
