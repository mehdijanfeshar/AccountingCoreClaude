using System.Globalization;
using Accounting.Domain.ValueObjects;
using FluentValidation;

namespace Accounting.Application.PettyCash.Commands.ReturnPettyCashExpenseDoc;

/// <summary>
/// Surface-level (syntactic) validation only. The business rule that <c>Id</c> must currently be
/// <see cref="PettyCashDocState.PendingReview"/> lives in
/// <c>Accounting.Application.PettyCash.Commands.Common.PettyCashReviewTransitionService</c>, not
/// here.
/// </summary>
public sealed class ReturnPettyCashExpenseDocCommandValidator : AbstractValidator<ReturnPettyCashExpenseDocCommand>
{
    /// <summary>Same <c>YYYYMMDD</c> Jalali convention as every other Legacy date column in this
    /// project (e.g. <c>GetWhiteAndBlackListsQueryValidator.LegacyJalaliDatePattern</c>).</summary>
    internal const string LegacyJalaliDatePattern = @"^\d{8}$";

    public ReturnPettyCashExpenseDocCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();

        RuleFor(x => x.ReasonCodes)
            .NotEmpty()
            .WithMessage("حداقل یک دلیل برگشت باید انتخاب شود.");

        RuleForEach(x => x.ReasonCodes)
            .Must(code => Enum.IsDefined(typeof(PettyCashReturnReason), code))
            .WithMessage("کد دلیل برگشت نامعتبر است.");

        RuleFor(x => x.Deadline)
            .NotEmpty()
            .Matches(LegacyJalaliDatePattern)
            .WithMessage("مهلت باید به‌صورت YYYYMMDD باشد.");

        // "تاریخی بعد از امروز" — plain ordinal string comparison is valid here because both sides
        // are same-length (8-digit) Jalali YYYYMMDD strings, exactly like every other Legacy date
        // range check in this project (e.g. ReactivateWhiteAndBlackListCommandValidator).
        RuleFor(x => x.Deadline)
            .Must(deadline => string.CompareOrdinal(deadline, TodayJalali()) > 0)
            .WithMessage("مهلت باید تاریخی بعد از امروز باشد.")
            .When(x => !string.IsNullOrEmpty(x.Deadline) && System.Text.RegularExpressions.Regex.IsMatch(x.Deadline, LegacyJalaliDatePattern));

        // صفحهٔ ۸ پاورپوینت: «توضیح برای تنخواه‌دار *» اجباری است.
        RuleFor(x => x.Note)
            .NotEmpty()
            .WithMessage("توضیح برای تنخواه‌دار الزامی است.")
            .MaximumLength(1000);
    }

    /// <summary>Today as a Legacy <c>YYYYMMDD</c> Jalali string — same approach as
    /// <c>ReverseVoucherCommandHandler.TodayJalali</c>.</summary>
    private static string TodayJalali()
    {
        var calendar = new PersianCalendar();
        var now = DateTime.Now;

        return $"{calendar.GetYear(now):0000}{calendar.GetMonth(now):00}{calendar.GetDayOfMonth(now):00}";
    }
}
