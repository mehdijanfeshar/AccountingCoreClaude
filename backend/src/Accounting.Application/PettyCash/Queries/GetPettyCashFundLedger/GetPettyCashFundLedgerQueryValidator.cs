using FluentValidation;

namespace Accounting.Application.PettyCash.Queries.GetPettyCashFundLedger;

public sealed class GetPettyCashFundLedgerQueryValidator : AbstractValidator<GetPettyCashFundLedgerQuery>
{
    private static readonly string[] AllowedTypes = { "replenishment", "expense", "refund" };

    public GetPettyCashFundLedgerQueryValidator()
    {
        RuleFor(x => x.FundId).NotEqual(Guid.Empty);

        RuleFor(x => x.From)
            .Matches("^[0-9]{8}$").WithMessage("تاریخ باید به شکل YYYYMMDD باشد.")
            .When(x => !string.IsNullOrWhiteSpace(x.From));

        RuleFor(x => x.To)
            .Matches("^[0-9]{8}$").WithMessage("تاریخ باید به شکل YYYYMMDD باشد.")
            .When(x => !string.IsNullOrWhiteSpace(x.To));

        RuleFor(x => x)
            .Must(x => string.CompareOrdinal(x.From, x.To) <= 0)
            .WithMessage("«از تاریخ» نباید بعد از «تا تاریخ» باشد.")
            .When(x => !string.IsNullOrWhiteSpace(x.From) && !string.IsNullOrWhiteSpace(x.To));

        RuleFor(x => x.Type)
            .Must(t => AllowedTypes.Contains(t))
            .WithMessage("نوع ردیف باید یکی از replenishment/expense/refund باشد.")
            .When(x => !string.IsNullOrWhiteSpace(x.Type));
    }
}
