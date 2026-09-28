using Accounting.Application.Common.Exceptions;
using Accounting.Application.Common.Interfaces;
using Accounting.Application.PettyCash.Commands.Common;
using Accounting.Domain.Entity;
using MediatR;

namespace Accounting.Application.PettyCash.Commands.CreatePettyCashRefund;

/// <summary>
/// Enforces «ثبت استرداد فقط برای کاربری که نقش متناظر (یا برای Custodian،
/// <c>CUSTODIAN_USERID</c>) همان تنخواه را دارد» via <see cref="IPettyCashRefundRecorderAuthorizer"/>
/// — <c>TB_PC_FUND.REFUND_RECORDER</c> decides which check applies, per fund (بخش ۳-الف،
/// <c>docs/tankhah-khazaneh-module.md</c>).
/// </summary>
public sealed class CreatePettyCashRefundCommandHandler : IRequestHandler<CreatePettyCashRefundCommand, Guid>
{
    private readonly IPettyCashFundRepository _fundRepository;
    private readonly IPettyCashRefundRepository _refundRepository;
    private readonly IPettyCashRefundRecorderAuthorizer _authorizer;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUser _currentUser;

    public CreatePettyCashRefundCommandHandler(
        IPettyCashFundRepository fundRepository,
        IPettyCashRefundRepository refundRepository,
        IPettyCashRefundRecorderAuthorizer authorizer,
        IUnitOfWork unitOfWork,
        ICurrentUser currentUser)
    {
        _fundRepository = fundRepository;
        _refundRepository = refundRepository;
        _authorizer = authorizer;
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
    }

    public async Task<Guid> Handle(CreatePettyCashRefundCommand request, CancellationToken cancellationToken)
    {
        var fund = await _fundRepository.GetForUpdateAsync(request.FundId, request.VahedCode, cancellationToken)
            ?? throw new NotFoundException("PettyCashFund", request.FundId);

        await _authorizer.EnsureCanRecordAsync(fund, cancellationToken);

        var userId = _currentUser.UserId;
        var now = DateTime.UtcNow;

        var nextCode = await _refundRepository.GetNextCodeAsync(request.VahedCode, cancellationToken);

        var refund = new TB_PC_REFUND
        {
            ID = Guid.NewGuid(),
            FUND_ID = request.FundId,
            CODE = "REF-" + nextCode.ToString("00000"),
            AMOUNT = request.Amount,
            REASON = request.Reason,
            REFUND_DATE = request.RefundDate,
            RECORDED_BY_USERID = userId,
            VAHEDCODE = request.VahedCode,
            ADDUSERID = userId,
            CREATEDDATE = now,
            ISDELETED = false,
        };

        await _refundRepository.AddAsync(refund, cancellationToken);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return refund.ID;
    }
}
