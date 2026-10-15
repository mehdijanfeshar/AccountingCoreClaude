using Accounting.Application.Common.Exceptions;

namespace Accounting.Api.Tests;

/// <summary>
/// گارد: هر استثنای Application که <c>PublicDetail</c> دارد باید <see cref="IPublicProblemException"/>
/// را پیاده کند؛ وگرنه <c>GlobalExceptionHandler</c> آن را بی‌صدا به ۵۰۰ می‌برد.
/// </summary>
public sealed class PublicProblemExceptionConventionTests
{
    // این‌ها شاخهٔ اختصاصی در handler دارند (extension به نام failedIds).
    private static readonly HashSet<string> HandledExplicitly =
    [
        "PettyCashBulkApproveConflictException",
        "PaymentRequestBulkApproveConflictException",
    ];

    [Fact]
    public void Every_exception_with_PublicDetail_implements_IPublicProblemException()
    {
        var offenders = typeof(IPublicProblemException).Assembly.GetTypes()
            .Where(t => typeof(Exception).IsAssignableFrom(t) && !t.IsAbstract)
            .Where(t => t.GetProperty("PublicDetail") is not null)
            .Where(t => !typeof(IPublicProblemException).IsAssignableFrom(t))
            .Where(t => !HandledExplicitly.Contains(t.Name))
            .Select(t => t.FullName)
            .ToList();

        Assert.True(offenders.Count == 0,
            "These exceptions have PublicDetail but do not implement IPublicProblemException " +
            "(they would surface as HTTP 500): " + string.Join(", ", offenders));
    }
}
