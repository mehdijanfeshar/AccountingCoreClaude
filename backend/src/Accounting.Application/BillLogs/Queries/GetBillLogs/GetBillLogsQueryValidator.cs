using FluentValidation;

namespace Accounting.Application.BillLogs.Queries.GetBillLogs;

/// <summary>
/// Surface-level (syntactic) pagination validation only.
/// </summary>
public sealed class GetBillLogsQueryValidator : AbstractValidator<GetBillLogsQuery>
{
    public const int MaxPageSize = 200;

    /// <summary>
    /// Upper bound for <c>PageNumber</c>, chosen so that <c>(pageNumber - 1) * pageSize</c> in
    /// <c>Skip(...)</c> can never overflow <see cref="int"/>.
    /// </summary>
    public const int MaxPageNumber = int.MaxValue / MaxPageSize;

    public GetBillLogsQueryValidator()
    {
        RuleFor(x => x.PageNumber)
            .InclusiveBetween(1, MaxPageNumber);

        RuleFor(x => x.PageSize)
            .InclusiveBetween(1, MaxPageSize);
    }
}
