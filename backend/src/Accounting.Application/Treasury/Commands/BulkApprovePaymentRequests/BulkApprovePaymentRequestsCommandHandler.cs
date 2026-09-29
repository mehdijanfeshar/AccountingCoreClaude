using Accounting.Application.Common.Exceptions;
using Accounting.Application.Common.Interfaces;
using Accounting.Application.Treasury.Commands.Common;
using MediatR;

namespace Accounting.Application.Treasury.Commands.BulkApprovePaymentRequests;

/// <summary>
/// Validates and stages every id's approval transition via
/// <see cref="IPaymentRequestApprovalService"/> before ever calling
/// <see cref="IUnitOfWork.SaveChangesAsync"/> — guarantees all-or-nothing without an explicit DB
/// transaction, same shape as <c>BulkApprovePettyCashExpenseDocsCommandHandler</c>. The per-id
/// <c>try/catch</c> below is a deliberate validation-aggregation pattern, not exception
/// swallowing — every case is a specific, expected business-rule outcome the batch API contract
/// requires reported per-id, and nothing is silently discarded.
/// </summary>
public sealed class BulkApprovePaymentRequestsCommandHandler : IRequestHandler<BulkApprovePaymentRequestsCommand>
{
    private readonly IPaymentRequestApprovalService _approvalService;
    private readonly IUnitOfWork _unitOfWork;

    public BulkApprovePaymentRequestsCommandHandler(IPaymentRequestApprovalService approvalService, IUnitOfWork unitOfWork)
    {
        _approvalService = approvalService;
        _unitOfWork = unitOfWork;
    }

    public async Task Handle(BulkApprovePaymentRequestsCommand request, CancellationToken cancellationToken)
    {
        var failures = new Dictionary<Guid, string>();

        foreach (var id in request.Ids.Distinct())
        {
            try
            {
                await _approvalService.ApproveAsync(id, request.VahedCode, note: null, isBulkApprove: true, cancellationToken);
            }
            catch (NotFoundException)
            {
                failures[id] = "not-found";
            }
            catch (TreasuryRoleRequiredException)
            {
                failures[id] = "forbidden";
            }
            catch (PaymentRequestRoleRequiredException)
            {
                failures[id] = "forbidden";
            }
            catch (PaymentRequestApproverConflictException)
            {
                failures[id] = "self-approve";
            }
            catch (PaymentRequestConsecutiveApproverConflictException)
            {
                failures[id] = "consecutive-approver";
            }
            catch (PaymentRequestStateConflictException)
            {
                failures[id] = "invalid-state";
            }
            catch (PaymentRequestSettingsMissingException)
            {
                failures[id] = "settings-missing";
            }
            catch (PaymentRequestBulkLimitExceededException)
            {
                failures[id] = "over-bulk-limit";
            }
        }

        if (failures.Count > 0)
        {
            // Thrown before SaveChangesAsync is ever reached, so nothing staged by the successful
            // iterations above is persisted either — the all-or-nothing guarantee.
            throw new PaymentRequestBulkApproveConflictException(failures);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
