using Accounting.Application.Common.Exceptions;
using Accounting.Application.Common.Interfaces;
using Accounting.Application.PettyCash.Commands.Common;
using MediatR;

namespace Accounting.Application.PettyCash.Commands.BulkApprovePettyCashExpenseDocs;

/// <summary>
/// Validates and stages every id's final-approval transition via
/// <see cref="IPettyCashFinalApprovalService"/> before ever calling
/// <see cref="IUnitOfWork.SaveChangesAsync"/> — see that interface's XML doc for why this alone
/// guarantees all-or-nothing without an explicit DB transaction. The per-id
/// <c>try/catch</c> below is a deliberate validation-aggregation pattern, not exception
/// swallowing: every case is a specific, expected business-rule outcome the batch API contract
/// requires to be reported per-id (<c>docs/tankhah-khazaneh-module.md</c>, تصمیم‌های بخش ۲), and
/// nothing here is silently discarded — it all reaches the caller inside
/// <see cref="PettyCashBulkApproveConflictException.Failures"/>. Any other exception type still
/// propagates immediately, uncaught, exactly like every other handler in this project.
/// </summary>
public sealed class BulkApprovePettyCashExpenseDocsCommandHandler : IRequestHandler<BulkApprovePettyCashExpenseDocsCommand>
{
    private readonly IPettyCashFinalApprovalService _finalApprovalService;
    private readonly IUnitOfWork _unitOfWork;

    public BulkApprovePettyCashExpenseDocsCommandHandler(
        IPettyCashFinalApprovalService finalApprovalService,
        IUnitOfWork unitOfWork)
    {
        _finalApprovalService = finalApprovalService;
        _unitOfWork = unitOfWork;
    }

    public async Task Handle(BulkApprovePettyCashExpenseDocsCommand request, CancellationToken cancellationToken)
    {
        var failures = new Dictionary<Guid, string>();

        foreach (var id in request.Ids.Distinct())
        {
            try
            {
                await _finalApprovalService.FinalApproveAsync(id, request.VahedCode, note: null, cancellationToken);
            }
            catch (NotFoundException)
            {
                failures[id] = "not-found";
            }
            catch (UnitAccessDeniedException)
            {
                failures[id] = "forbidden";
            }
            catch (PettyCashReviewerAccessDeniedException)
            {
                failures[id] = "forbidden";
            }
            catch (PettyCashSelfReviewConflictException)
            {
                failures[id] = "self-review";
            }
            catch (PettyCashVerifierCannotApproveException)
            {
                failures[id] = "self-review";
            }
            catch (PettyCashReviewStateConflictException)
            {
                failures[id] = "invalid-state";
            }
            catch (PettyCashNotVerifiedException)
            {
                failures[id] = "not-verified";
            }
            catch (PettyCashApprovalAuthorityExceededException)
            {
                failures[id] = "over-authority";
            }
        }

        if (failures.Count > 0)
        {
            // Thrown before SaveChangesAsync is ever reached, so nothing staged by the
            // successful iterations above is persisted either — the all-or-nothing guarantee.
            throw new PettyCashBulkApproveConflictException(failures);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
