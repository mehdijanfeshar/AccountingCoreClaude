using Accounting.Application.WhiteAndBlackLists.Queries.GetWhiteAndBlackLists;
using Accounting.Domain.ValueObjects;
using FluentValidation;

namespace Accounting.Application.WhiteAndBlackLists.Commands.CreateWhiteAndBlackListsBulk;

/// <summary>
/// Surface-level (syntactic) validation only, matching the Fluent mapping constraints in
/// <c>LegacyDbContext</c>. Per the recorded "Legacy fully replaces the rich model" architecture
/// decision, accounting invariants were deliberately discarded and must NOT be re-created here.
/// </summary>
public sealed class CreateWhiteAndBlackListsBulkCommandValidator : AbstractValidator<CreateWhiteAndBlackListsBulkCommand>
{
    /// <summary>
    /// Ceiling on the cartesian product a single request may expand to. Live data bounds the
    /// realistic worst case at 60 معین × 17 نوع واحد ≈ 1020, so this leaves ample headroom while
    /// still refusing a request that would stage an unbounded number of rows in one transaction.
    /// </summary>
    public const int MaxCombinations = 5_000;

    public CreateWhiteAndBlackListsBulkCommandValidator()
    {
        RuleFor(x => x.AccountCodeIds)
            .NotEmpty();

        RuleForEach(x => x.AccountCodeIds)
            .NotEqual(Guid.Empty);

        RuleFor(x => x.VahedTypeIds)
            .NotEmpty();

        RuleForEach(x => x.VahedTypeIds)
            .NotEqual(Guid.Empty);

        RuleFor(x => x)
            .Must(x => (long)x.AccountCodeIds.Distinct().Count() * x.VahedTypeIds.Distinct().Count() <= MaxCombinations)
            .WithMessage($"تعداد ترکیب‌های حساب × نوع واحد نمی‌تواند از {MaxCombinations} بیشتر باشد.")
            .When(x => x.AccountCodeIds.Count > 0 && x.VahedTypeIds.Count > 0);

        RuleFor(x => x.FromDate)
            .Matches(GetWhiteAndBlackListsQueryValidator.LegacyJalaliDatePattern)
            .When(x => !string.IsNullOrEmpty(x.FromDate));

        RuleFor(x => x.ToDate)
            .Matches(GetWhiteAndBlackListsQueryValidator.LegacyJalaliDatePattern)
            .When(x => !string.IsNullOrEmpty(x.ToDate));

        // Both values are 8-char YYYYMMDD Jalali text, so an ordinal string comparison orders them
        // chronologically — the same property the list filters rely on.
        RuleFor(x => x)
            .Must(x => string.CompareOrdinal(x.FromDate, x.ToDate) <= 0)
            .WithMessage("«از تاریخ» نمی‌تواند بعد از «تا تاریخ» باشد.")
            .When(x => !string.IsNullOrEmpty(x.FromDate) && !string.IsNullOrEmpty(x.ToDate));

        RuleFor(x => x.State)
            .IsInEnum();

        // Blacklisting is a transition applied to an existing row, never an initial state — see
        // the command's XML doc.
        RuleFor(x => x.State)
            .NotEqual(WhiteBlackListState.Blacklisted)
            .WithMessage("برای غیرمجاز کردن، از عملیات «غیرفعال‌سازی» روی ردیف موجود استفاده کنید.");
    }
}
