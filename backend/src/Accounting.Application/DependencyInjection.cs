using System.Reflection;
using Accounting.Application.Common.Behaviors;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.DependencyInjection;

namespace Accounting.Application;

public static class DependencyInjection
{
    /// <summary>
    /// Registers MediatR (Commands/Queries/Handlers), all FluentValidation validators found
    /// in this assembly, and the pipeline behaviors that run before every request reaches its
    /// handler: <see cref="VahedScopeBehavior{TRequest,TResponse}"/> then
    /// <see cref="ValidationBehavior{TRequest,TResponse}"/>.
    ///
    /// <b>Registration order matters.</b> MediatR runs registered <c>IPipelineBehavior</c>
    /// instances outermost-first, in registration order, so <c>VahedScopeBehavior</c> — being
    /// registered first — wraps <c>ValidationBehavior</c>, not the other way round. This is
    /// deliberate: authorization (who is allowed to write into which unit) must be resolved
    /// before syntactic validation runs, and concretely, any <c>RuleFor(x => x.VahedCode)</c> in
    /// a FluentValidation validator must see the server-assigned value, not whatever the caller
    /// sent — if <c>ValidationBehavior</c> ran first, it would validate a value that
    /// <c>VahedScopeBehavior</c> is about to discard and replace.
    ///
    /// ⚠️ <b>Separately discovered, pre-existing bug — recorded here, not fixed here (out of
    /// this change's scope):</b> <c>ValidationBehavior&lt;TRequest,TResponse&gt;</c> declares
    /// <c>where TRequest : IRequest&lt;TResponse&gt;</c>. Verified empirically against this
    /// project's actual MediatR 14.2.0 that a void command (<c>: IRequest</c>, no generic
    /// response — every <c>Update</c>/<c>Delete</c> command in this project) does not implement
    /// <c>IRequest&lt;Unit&gt;</c> in this MediatR version, so the .NET DI container's
    /// open-generic resolution silently skips <c>ValidationBehavior</c> for every such request —
    /// it currently never runs for any Update/Delete command in the whole application. See
    /// <c>VahedScopeBehavior</c>'s XML doc for the full mechanism and why that class deliberately
    /// avoids the same constraint.
    /// </summary>
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        var applicationAssembly = Assembly.GetExecutingAssembly();

        services.AddMediatR(configuration => configuration.RegisterServicesFromAssembly(applicationAssembly));

        services.AddValidatorsFromAssembly(applicationAssembly);

        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(VahedScopeBehavior<,>));
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));

        return services;
    }
}
