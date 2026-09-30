using Accounting.Application.Common.Exceptions;
using Accounting.Application.Common.Interfaces;
using Accounting.Application.Treasury.Commands.Common;
using Accounting.Domain.Entity;
using Accounting.Domain.ValueObjects;
using MediatR;

namespace Accounting.Application.Treasury.Commands.CreateBankStatement;

public sealed class CreateBankStatementCommandHandler : IRequestHandler<CreateBankStatementCommand, Guid>
{
    private static readonly TreasuryRole[] EditorRoles = { TreasuryRole.Treasurer, TreasuryRole.SeniorAccountant };

    private readonly ITreasuryBankStatementRepository _statementRepository;
    private readonly IBankAccountReadRepository _bankAccountReadRepository;
    private readonly ITreasuryRoleAuthorizer _roleAuthorizer;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUser _currentUser;

    public CreateBankStatementCommandHandler(
        ITreasuryBankStatementRepository statementRepository,
        IBankAccountReadRepository bankAccountReadRepository,
        ITreasuryRoleAuthorizer roleAuthorizer,
        IUnitOfWork unitOfWork,
        ICurrentUser currentUser)
    {
        _statementRepository = statementRepository;
        _bankAccountReadRepository = bankAccountReadRepository;
        _roleAuthorizer = roleAuthorizer;
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
    }

    public async Task<Guid> Handle(CreateBankStatementCommand request, CancellationToken cancellationToken)
    {
        await _roleAuthorizer.EnsureHasRoleAsync(request.VahedCode, EditorRoles, cancellationToken);

        _ = await _bankAccountReadRepository.GetByIdAsync(request.BankAccountId, request.VahedCode, cancellationToken)
            ?? throw new NotFoundException("BankAccount", request.BankAccountId);

        var now = DateTime.UtcNow;
        var userId = _currentUser.UserId;

        var nextCode = await _statementRepository.GetNextCodeAsync(request.VahedCode, request.Year, cancellationToken);

        var statement = new TB_TR_BANK_STATEMENT
        {
            ID = Guid.NewGuid(),
            CODE = "BST-" + nextCode.ToString("000000"),
            BANK_ACCOUNT_ID = request.BankAccountId,
            FROM_DATE = request.FromDate,
            TO_DATE = request.ToDate,
            CLOSING_BALANCE = request.ClosingBalance,
            SOURCE = BankStatementSource.Manual,
            STATE = BankStatementState.Open,
            DESCRIPTION = request.Description,
            VAHEDCODE = request.VahedCode,
            YEAR = request.Year,
            ADDUSERID = userId,
            CREATEDDATE = now,
            ISDELETED = false,
        };

        await _statementRepository.AddAsync(statement, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return statement.ID;
    }
}
