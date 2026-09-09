using Accounting.Application.Common.Interfaces;
using Accounting.Domain.Entity;
using MediatR;

namespace Accounting.Application.ChequeTypes.Commands.CreateChequeType;

/// <summary>
/// Constructs the <see cref="TB_CHECK_TYPE"/> Domain entity from the command, stages it via
/// <see cref="IChequeTypeRepository"/>, and owns the transaction boundary by calling
/// <see cref="IUnitOfWork.SaveChangesAsync"/> exactly once. <c>ADDUSERID</c> is sourced from
/// <see cref="ICurrentUser"/> (the authenticated caller) — never from the request.
/// <c>request.VahedCode</c> is equally non-forgeable: by the time this handler runs,
/// <c>VahedScopeBehavior</c> has already overwritten it with the authenticated caller's own unit
/// code (see <see cref="CreateChequeTypeCommand.VahedCode"/>).
///
/// <c>ID</c> is always generated application-side (<see cref="Guid.NewGuid"/>).
/// </summary>
public sealed class CreateChequeTypeCommandHandler : IRequestHandler<CreateChequeTypeCommand, Guid>
{
    private readonly IChequeTypeRepository _chequeTypeRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUser _currentUser;

    public CreateChequeTypeCommandHandler(
        IChequeTypeRepository chequeTypeRepository,
        IUnitOfWork unitOfWork,
        ICurrentUser currentUser)
    {
        _chequeTypeRepository = chequeTypeRepository;
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
    }

    public async Task<Guid> Handle(CreateChequeTypeCommand request, CancellationToken cancellationToken)
    {
        var entity = new TB_CHECK_TYPE
        {
            ID = Guid.NewGuid(),
            CHEQUE_TYPE_TITLE = request.ChequeTypeTitle,
            CHEQUE_WIDTH = request.ChequeWidth,
            CHEQUE_HEIGHT = request.ChequeHeight,
            CHEQUE_IMAGE = request.ChequeImage,
            CHEQUE_ADATE_FONT = request.ChequeAdateFont,
            CHEQUE_ADATE_LEFT = request.ChequeAdateLeft,
            CHEQUE_ADATE_TOP = request.ChequeAdateTop,
            CHEQUE_ADATE_WIDTH = request.ChequeAdateWidth,
            CHEQUE_NDATE_FONT = request.ChequeNdateFont,
            CHEQUE_NDATE_LEFT = request.ChequeNdateLeft,
            CHEQUE_NDATE_TOP = request.ChequeNdateTop,
            CHEQUE_NDATE_WIDTH = request.ChequeNdateWidth,
            CHEQUE_AAMOUNT_FONT = request.ChequeAamountFont,
            CHEQUE_AAMOUNT_LEFT = request.ChequeAamountLeft,
            CHEQUE_AAMOUNT_TOP = request.ChequeAamountTop,
            CHEQUE_AAMOUNT_WIDTH = request.ChequeAamountWidth,
            CHEQUE_LAMOUNT_FONT = request.ChequeLamountFont,
            CHEQUE_LAMOUNT_LEFT = request.ChequeLamountLeft,
            CHEQUE_LAMOUNT_TOP = request.ChequeLamountTop,
            CHEQUE_LAMOUNT_WIDTH = request.ChequeLamountWidth,
            CHEQUE_NAMOUNT_FONT = request.ChequeNamountFont,
            CHEQUE_NAMOUNT_LEFT = request.ChequeNamountLeft,
            CHEQUE_NAMOUNT_TOP = request.ChequeNamountTop,
            CHEQUE_NAMOUNT_WIDTH = request.ChequeNamountWidth,
            CHEQUE_DESCRIBE1_FONT = request.ChequeDescribe1Font,
            CHEQUE_DESCRIBE1_LEFT = request.ChequeDescribe1Left,
            CHEQUE_DESCRIBE1_TOP = request.ChequeDescribe1Top,
            CHEQUE_DESCRIBE1_WIDTH = request.ChequeDescribe1Width,
            CHEQUE_DESCRIBE2_FONT = request.ChequeDescribe2Font,
            CHEQUE_DESCRIBE2_LEFT = request.ChequeDescribe2Left,
            CHEQUE_DESCRIBE2_TOP = request.ChequeDescribe2Top,
            CHEQUE_DESCRIBE2_WIDTH = request.ChequeDescribe2Width,
            CHEQUE_BREAKLINE_FONT = request.ChequeBreaklineFont,
            CHEQUE_BREAKLINE_LEFT = request.ChequeBreaklineLeft,
            CHEQUE_BREAKLINE_TOP = request.ChequeBreaklineTop,
            CHEQUE_BREAKLINE_WIDTH = request.ChequeBreaklineWidth,
            PRINTER_MARGINE_TOP = request.PrinterMargineTop,
            PRINTER_MARGINE_LEFT = request.PrinterMargineLeft,
            PRINTER_TYPE = request.PrinterType,
            YEAR = request.Year,
            VAHEDCODE = request.VahedCode,
            ADDUSERID = _currentUser.UserId,
            CREATEDDATE = DateTime.UtcNow,
            ISDELETED = false,
        };

        await _chequeTypeRepository.AddAsync(entity, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return entity.ID;
    }
}
