using Accounting.Application.Common.Exceptions;
using Accounting.Application.Common.Interfaces;
using Accounting.Application.Treasury.Commands.Common;
using Accounting.Domain.Entity;
using Accounting.Domain.ValueObjects;
using MediatR;

namespace Accounting.Application.Treasury.Commands.ExecutePaymentRequest;

/// <summary>
/// Sensitive multi-write use case (GL voucher + Legacy PayReciv head/detail + state transition) —
/// wrapped in an explicit <see cref="IUnitOfWork.BeginTransactionAsync"/>/<see cref="IUnitOfWork.CommitTransactionAsync"/>
/// pair per CLAUDE.md's rule for سند/تولید شماره use cases, same shape as
/// <c>FinalizePettyCashSettlementCommandHandler</c>. Every validation runs BEFORE
/// <see cref="IPaymentRequestPaymentVoucherBuilder.BuildAndStageAsync"/> is called.
/// </summary>
public sealed class ExecutePaymentRequestCommandHandler : IRequestHandler<ExecutePaymentRequestCommand>
{
    private readonly IPaymentRequestRepository _paymentRequestRepository;
    private readonly IPaymentRequestEventRepository _eventRepository;
    private readonly IPaymentRequestExecutionAuthorizer _executionAuthorizer;
    private readonly IPaymentRequestPaymentVoucherBuilder _paymentVoucherBuilder;
    private readonly IPayReciveHeadRepository _payReciveHeadRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUser _currentUser;
    private readonly IClientInfoProvider _clientInfoProvider;

    public ExecutePaymentRequestCommandHandler(
        IPaymentRequestRepository paymentRequestRepository,
        IPaymentRequestEventRepository eventRepository,
        IPaymentRequestExecutionAuthorizer executionAuthorizer,
        IPaymentRequestPaymentVoucherBuilder paymentVoucherBuilder,
        IPayReciveHeadRepository payReciveHeadRepository,
        IUnitOfWork unitOfWork,
        ICurrentUser currentUser,
        IClientInfoProvider clientInfoProvider)
    {
        _paymentRequestRepository = paymentRequestRepository;
        _eventRepository = eventRepository;
        _executionAuthorizer = executionAuthorizer;
        _paymentVoucherBuilder = paymentVoucherBuilder;
        _payReciveHeadRepository = payReciveHeadRepository;
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
        _clientInfoProvider = clientInfoProvider;
    }

    public async Task Handle(ExecutePaymentRequestCommand request, CancellationToken cancellationToken)
    {
        var paymentRequest = await _paymentRequestRepository.GetForUpdateAsync(request.Id, request.VahedCode, cancellationToken);

        if (paymentRequest is null || paymentRequest.ISDELETED)
        {
            throw new NotFoundException("PaymentRequest", request.Id);
        }

        if (paymentRequest.REQUEST_STATE != PaymentRequestState.ReadyForExecution)
        {
            throw new PaymentRequestStateConflictException(request.Id, paymentRequest.REQUEST_STATE, "آمادهٔ اجرا");
        }

        await _executionAuthorizer.EnsureTreasurerAsync(request.Id, request.VahedCode, cancellationToken);

        // SoD (owner decision ۲۰۲۶-۰۹-۲۹): اجراکننده ≠ ثبت‌کنندهٔ درخواست.
        if (string.Equals(paymentRequest.ADDUSERID, _currentUser.UserId, StringComparison.Ordinal))
        {
            throw new PaymentRequestExecutorConflictException(request.Id);
        }

        var userId = _currentUser.UserId;
        var now = DateTime.UtcNow;

        await _unitOfWork.BeginTransactionAsync(cancellationToken);

        try
        {
            var voucherResult = await _paymentVoucherBuilder.BuildAndStageAsync(
                paymentRequest, request.PaidDate, request.VahedCode, cancellationToken);

            var nextCode = await _payReciveHeadRepository.GetNextCodeAsync(request.VahedCode, voucherResult.Year, cancellationToken);

            var payRecivHead = new TB_PAYRECIVHEAD
            {
                ID = Guid.NewGuid(),
                PAYRECIVCODE = nextCode.ToString("00000"),
                PAYRECIVDATE = request.PaidDate,
                PAYRECIVDESCRIPTION = $"پرداخت — {paymentRequest.CODE} — {paymentRequest.BENEFICIARY_NAME}",
                PAYRECIVTYPE = PayRecivType.Pay,
                VAHEDCODE = request.VahedCode,
                YEAR = voucherResult.Year,
                VOUCHERSHEAD_ID = voucherResult.VoucherHeadId,
                ADDUSERID = userId,
                CREATEDDATE = now,
                ISDELETED = false,
            };

            await _payReciveHeadRepository.AddAsync(payRecivHead, cancellationToken);

            var radif = 0;

            foreach (var line in voucherResult.Lines)
            {
                radif++;

                var detail = new TB_PAYRECIVDETAIL
                {
                    ID = Guid.NewGuid(),
                    ACCOUNTCODE_ID = line.AccountCodeId,
                    ARTICLEDESCRIPTION = line.Debit > 0 ? "پرداخت — بستانکاران" : "پرداخت — بانک",
                    RADIF = radif,
                    DEBTOR = line.Debit,
                    CREDITOR = line.Credit,
                    VAHEDCODE = request.VahedCode,
                    YEAR = voucherResult.Year,
                    PAYRECIVHEAD_ID = payRecivHead.ID,
                    ADDUSERID = userId,
                    CREATEDDATE = now,
                    ISDELETED = false,
                };

                await _payReciveHeadRepository.AddDetailAsync(detail, cancellationToken);

                foreach (var tafsili in line.TafsiliLinks)
                {
                    await _payReciveHeadRepository.AddDetailTafsiliLinkAsync(
                        new TB_PAYRECIVDETAIL_LINK_TAFSILI
                        {
                            ID = Guid.NewGuid(),
                            PAYRECIVDETAIL_ID = detail.ID,
                            TAFSILI_ID = tafsili.TafsiliId,
                            LEVEL_ID = tafsili.LevelId,
                            VAHEDCODE = request.VahedCode,
                            YEAR = voucherResult.Year,
                            ADDUSERID = userId,
                            CREATEDDATE = now,
                            ISDELETED = false,
                        },
                        cancellationToken);
                }
            }

            paymentRequest.PAYMENT_VOUCHER_ID = voucherResult.VoucherHeadId;
            paymentRequest.PAYRECIVHEAD_ID = payRecivHead.ID;
            paymentRequest.BANK_REFERENCE = request.BankReference;
            paymentRequest.PAID_DATE = request.PaidDate;
            paymentRequest.DESTINATION_IBAN = request.DestinationIban;

            if (request.PaymentMethod is { } paymentMethod)
            {
                paymentRequest.PAYMENT_METHOD = paymentMethod;
            }

            paymentRequest.EXECUTED_BY = userId;
            paymentRequest.EXECUTED_DATE = now;
            paymentRequest.REQUEST_STATE = PaymentRequestState.Executed;
            paymentRequest.CHANGEUSERID = userId;
            paymentRequest.UPDATEDDATE = now;

            await _eventRepository.AddAsync(
                new TB_TR_PAYMENT_REQUEST_EVENT
                {
                    ID = Guid.NewGuid(),
                    PAYMENT_REQUEST_ID = paymentRequest.ID,
                    ACTION = PaymentRequestEventAction.Execute,
                    FROM_STATE = PaymentRequestState.ReadyForExecution,
                    TO_STATE = PaymentRequestState.Executed,
                    NOTE = $"سند پرداخت شمارهٔ {voucherResult.DocNum} صادر شد — مرجع بانکی {request.BankReference}.",
                    CLIENT_IP = _clientInfoProvider.ClientIp,
                    VAHEDCODE = request.VahedCode,
                    YEAR = paymentRequest.YEAR,
                    ADDUSERID = userId,
                    CREATEDDATE = now,
                    ISDELETED = false,
                },
                cancellationToken);

            await _unitOfWork.SaveChangesAsync(cancellationToken);
            await _unitOfWork.CommitTransactionAsync(cancellationToken);
        }
        catch
        {
            await _unitOfWork.RollbackTransactionAsync(cancellationToken);
            throw;
        }
    }
}
