using Accounting.Application.Common.Exceptions;
using Accounting.Application.Common.Interfaces;
using MediatR;

namespace Accounting.Application.ChequeTypes.Commands.UpdateChequeType;

/// <summary>
/// Loads the existing <see cref="Accounting.Domain.Entity.TB_CHECK_TYPE"/> row via
/// <see cref="IChequeTypeRepository.GetForUpdateAsync"/> (change-tracked), overwrites every
/// writable field from the command, stamps audit columns, and owns the transaction boundary by
/// calling <see cref="IUnitOfWork.SaveChangesAsync"/> exactly once. Throws
/// <see cref="NotFoundException"/> — mapped to 404 by <c>GlobalExceptionHandler</c> — when the
/// row does not exist or is already soft-deleted (<c>ISDELETED</c> is non-nullable <c>bool</c>
/// on this table). <c>CHANGEUSERID</c> is sourced from <see cref="ICurrentUser"/> — never from
/// the request. <c>request.VahedCode</c> is equally non-forgeable: by the time this handler
/// runs, <c>VahedScopeBehavior</c> has already overwritten it with the authenticated caller's
/// own unit code (see <see cref="UpdateChequeTypeCommand.VahedCode"/>).
/// </summary>
public sealed class UpdateChequeTypeCommandHandler : IRequestHandler<UpdateChequeTypeCommand>
{
    private readonly IChequeTypeRepository _chequeTypeRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUser _currentUser;

    public UpdateChequeTypeCommandHandler(
        IChequeTypeRepository chequeTypeRepository,
        IUnitOfWork unitOfWork,
        ICurrentUser currentUser)
    {
        _chequeTypeRepository = chequeTypeRepository;
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
    }

    public async Task Handle(UpdateChequeTypeCommand request, CancellationToken cancellationToken)
    {
        var entity = await _chequeTypeRepository.GetForUpdateAsync(request.Id, cancellationToken);

        if (entity is null || entity.ISDELETED)
        {
            throw new NotFoundException("ChequeType", request.Id);
        }

        entity.CHEQUE_TYPE_TITLE = request.ChequeTypeTitle;
        entity.CHEQUE_WIDTH = request.ChequeWidth;
        entity.CHEQUE_HEIGHT = request.ChequeHeight;
        entity.CHEQUE_IMAGE = request.ChequeImage;
        entity.CHEQUE_ADATE_FONT = request.ChequeAdateFont;
        entity.CHEQUE_ADATE_LEFT = request.ChequeAdateLeft;
        entity.CHEQUE_ADATE_TOP = request.ChequeAdateTop;
        entity.CHEQUE_ADATE_WIDTH = request.ChequeAdateWidth;
        entity.CHEQUE_NDATE_FONT = request.ChequeNdateFont;
        entity.CHEQUE_NDATE_LEFT = request.ChequeNdateLeft;
        entity.CHEQUE_NDATE_TOP = request.ChequeNdateTop;
        entity.CHEQUE_NDATE_WIDTH = request.ChequeNdateWidth;
        entity.CHEQUE_AAMOUNT_FONT = request.ChequeAamountFont;
        entity.CHEQUE_AAMOUNT_LEFT = request.ChequeAamountLeft;
        entity.CHEQUE_AAMOUNT_TOP = request.ChequeAamountTop;
        entity.CHEQUE_AAMOUNT_WIDTH = request.ChequeAamountWidth;
        entity.CHEQUE_LAMOUNT_FONT = request.ChequeLamountFont;
        entity.CHEQUE_LAMOUNT_LEFT = request.ChequeLamountLeft;
        entity.CHEQUE_LAMOUNT_TOP = request.ChequeLamountTop;
        entity.CHEQUE_LAMOUNT_WIDTH = request.ChequeLamountWidth;
        entity.CHEQUE_NAMOUNT_FONT = request.ChequeNamountFont;
        entity.CHEQUE_NAMOUNT_LEFT = request.ChequeNamountLeft;
        entity.CHEQUE_NAMOUNT_TOP = request.ChequeNamountTop;
        entity.CHEQUE_NAMOUNT_WIDTH = request.ChequeNamountWidth;
        entity.CHEQUE_DESCRIBE1_FONT = request.ChequeDescribe1Font;
        entity.CHEQUE_DESCRIBE1_LEFT = request.ChequeDescribe1Left;
        entity.CHEQUE_DESCRIBE1_TOP = request.ChequeDescribe1Top;
        entity.CHEQUE_DESCRIBE1_WIDTH = request.ChequeDescribe1Width;
        entity.CHEQUE_DESCRIBE2_FONT = request.ChequeDescribe2Font;
        entity.CHEQUE_DESCRIBE2_LEFT = request.ChequeDescribe2Left;
        entity.CHEQUE_DESCRIBE2_TOP = request.ChequeDescribe2Top;
        entity.CHEQUE_DESCRIBE2_WIDTH = request.ChequeDescribe2Width;
        entity.CHEQUE_BREAKLINE_FONT = request.ChequeBreaklineFont;
        entity.CHEQUE_BREAKLINE_LEFT = request.ChequeBreaklineLeft;
        entity.CHEQUE_BREAKLINE_TOP = request.ChequeBreaklineTop;
        entity.CHEQUE_BREAKLINE_WIDTH = request.ChequeBreaklineWidth;
        entity.PRINTER_MARGINE_TOP = request.PrinterMargineTop;
        entity.PRINTER_MARGINE_LEFT = request.PrinterMargineLeft;
        entity.PRINTER_TYPE = request.PrinterType;
        entity.YEAR = request.Year;
        entity.VAHEDCODE = request.VahedCode;
        entity.CHANGEUSERID = _currentUser.UserId;
        entity.UPDATEDDATE = DateTime.UtcNow;

        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
