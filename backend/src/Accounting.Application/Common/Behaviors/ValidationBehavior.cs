using FluentValidation;
using MediatR;

namespace Accounting.Application.Common.Behaviors;

/// <summary>
/// MediatR pipeline behavior that runs all registered FluentValidation validators for the
/// incoming request before it reaches its handler. Throws <see cref="ValidationException"/>
/// on the first failing request, so handlers never need to re-implement field-level
/// validation. This is the standard validation pattern for every future Command/Query.
///
/// ⚠️ <b>Do not "tidy" the type parameters back to <c>where TRequest : IRequest&lt;TResponse&gt;</c>.</b>
/// That constraint is what this class shipped with from phase 5 until it was removed, and under
/// MediatR 14 it silently disabled the entire behavior for void commands: a command declared
/// <c>: IRequest</c> (no generic response — every <c>Update</c>/<c>Delete</c>/batch command here)
/// does not implement <c>IRequest&lt;Unit&gt;</c> in this MediatR version, so the DI container's
/// open-generic resolution could not close the registration and skipped it — no exception, no
/// log. The practical effect, live from phase 8 to phase 30: <b>every FluentValidation validator
/// written for an Update or Delete command was registered, was covered by its own unit tests, and
/// was never once invoked by the pipeline.</b> Dropping the constraint to the bare
/// <c>notnull</c> that <see cref="IPipelineBehavior{TRequest,TResponse}"/> itself requires lets
/// MediatR's void-request dispatch (which really does invoke
/// <c>IPipelineBehavior&lt;TRequest, Unit&gt;</c>) resolve it for both request shapes.
/// <see cref="VahedScopeBehavior{TRequest,TResponse}"/> carries the full mechanism and the
/// empirical MediatR 14.2.0 probe that established it; it has always been written this way
/// deliberately, for the same reason.
/// </summary>
public sealed class ValidationBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private readonly IEnumerable<IValidator<TRequest>> _validators;

    public ValidationBehavior(IEnumerable<IValidator<TRequest>> validators)
    {
        _validators = validators;
    }

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        if (!_validators.Any())
        {
            return await next(cancellationToken);
        }

        var context = new ValidationContext<TRequest>(request);

        var failures = (await Task.WhenAll(
                _validators.Select(validator => validator.ValidateAsync(context, cancellationToken))))
            .SelectMany(result => result.Errors)
            .Where(failure => failure is not null)
            .ToList();

        if (failures.Count != 0)
        {
            throw new ValidationException(failures);
        }

        return await next(cancellationToken);
    }
}
