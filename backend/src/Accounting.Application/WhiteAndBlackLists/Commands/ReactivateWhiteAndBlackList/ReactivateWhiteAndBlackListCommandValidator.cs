using Accounting.Application.WhiteAndBlackLists.Queries.GetWhiteAndBlackLists;
using Accounting.Domain.ValueObjects;
using FluentValidation;

namespace Accounting.Application.WhiteAndBlackLists.Commands.ReactivateWhiteAndBlackList;

/// <summary>
/// Surface-level (syntactic) validation only, matching the Fluent mapping constraints in
/// <c>LegacyDbContext</c>.
/// </summary>
public sealed class ReactivateWhiteAndBlackListCommandValidator : AbstractValidator<ReactivateWhiteAndBlackListCommand>
{
    public ReactivateWhiteAndBlackListCommandValidator()
    {
        RuleFor(x => x.Id)
            .NotEqual(Guid.Empty);

        RuleFor(x => x.State)
            .IsInEnum();

        // Reactivating INTO the blacklisted state is a contradiction in terms; «غیرفعال‌سازی» is
        // the operation for that.
        RuleFor(x => x.State)
            .NotEqual(WhiteBlackListState.Blacklisted)
            .WithMessage("وضعیت «فعال‌سازی مجدد» نمی‌تواند «غیرمجاز» باشد.");

        RuleFor(x => x.FromDate)
            .Matches(GetWhiteAndBlackListsQueryValidator.LegacyJalaliDatePattern)
            .When(x => !string.IsNullOrEmpty(x.FromDate));

        RuleFor(x => x.ToDate)
            .Matches(GetWhiteAndBlackListsQueryValidator.LegacyJalaliDatePattern)
            .When(x => !string.IsNullOrEmpty(x.ToDate));

        RuleFor(x => x)
            .Must(x => string.CompareOrdinal(x.FromDate, x.ToDate) <= 0)
            .WithMessage("«از تاریخ» نمی‌تواند بعد از «تا تاریخ» باشد.")
            .When(x => !string.IsNullOrEmpty(x.FromDate) && !string.IsNullOrEmpty(x.ToDate));
    }
}
