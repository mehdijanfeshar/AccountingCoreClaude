using System.Globalization;
using Accounting.Application.Common.Exceptions;
using Accounting.Application.Common.Interfaces;
using MediatR;

namespace Accounting.Application.Treasury.Queries.GetBankAccountBalance;

public sealed class GetBankAccountBalanceQueryHandler : IRequestHandler<GetBankAccountBalanceQuery, BankAccountBalanceDto?>
{
    private readonly IBankAccountReadRepository _bankAccountReadRepository;
    private readonly ITreasuryBankAccountBalanceReadRepository _balanceReadRepository;

    public GetBankAccountBalanceQueryHandler(
        IBankAccountReadRepository bankAccountReadRepository,
        ITreasuryBankAccountBalanceReadRepository balanceReadRepository)
    {
        _bankAccountReadRepository = bankAccountReadRepository;
        _balanceReadRepository = balanceReadRepository;
    }

    public async Task<BankAccountBalanceDto?> Handle(GetBankAccountBalanceQuery request, CancellationToken cancellationToken)
    {
        var bankAccount = await _bankAccountReadRepository.GetByIdAsync(request.Id, request.VahedCode, cancellationToken);

        if (bankAccount is null)
        {
            return null;
        }

        var accountCodeId = bankAccount.AccountCodeId
            ?? throw new NotFoundException("BankAccountCode", request.Id);

        var year = TodayJalaliYear();

        var tafsiliIds = bankAccount.TafsiliLinks.Select(l => l.TafsiliId).ToList();
        var balance = await _balanceReadRepository.GetBalanceAsync(
            accountCodeId, tafsiliIds, request.VahedCode, year, cancellationToken);

        return new BankAccountBalanceDto(request.Id, balance);
    }

    /// <summary>Current شمسی year as <c>YYYY</c> — same <c>PersianCalendar</c> approach as
    /// <c>PaymentRequestLiabilityVoucherBuilder.TodayJalali</c>.</summary>
    private static string TodayJalaliYear()
    {
        var calendar = new PersianCalendar();
        var now = DateTime.Now;

        return calendar.GetYear(now).ToString("0000", CultureInfo.InvariantCulture);
    }
}
