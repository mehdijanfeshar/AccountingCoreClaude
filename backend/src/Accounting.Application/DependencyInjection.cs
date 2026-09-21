using System.Reflection;
using Accounting.Application.Accounts.Commands.Common;
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
    /// ✅ <b>Fixed in phase 31 — both behaviors now declare only <c>where TRequest : notnull</c>.</b>
    /// <c>ValidationBehavior</c> used to declare <c>where TRequest : IRequest&lt;TResponse&gt;</c>,
    /// which (verified empirically against this project's actual MediatR 14.2.0) is not satisfied
    /// by a void command — <c>: IRequest</c> with no generic response, i.e. every
    /// <c>Update</c>/<c>Delete</c> command here — because <c>IRequest</c> does not implement
    /// <c>IRequest&lt;Unit&gt;</c> in this MediatR version. The .NET DI container's open-generic
    /// resolution skipped the registration silently, so from phase 8 to phase 30 no Update or
    /// Delete command was ever validated by the pipeline. <b>Do not reintroduce that constraint
    /// on either behavior</b> — on <c>ValidationBehavior</c> it disables validation, on
    /// <c>VahedScopeBehavior</c> it reopens IDOR risk #1; <c>BehaviorPipelineConstraintTests</c>
    /// fails if either one grows a constraint again.
    /// </summary>
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        var applicationAssembly = Assembly.GetExecutingAssembly();

        services.AddMediatR(configuration => configuration.RegisterServicesFromAssembly(applicationAssembly));

        services.AddValidatorsFromAssembly(applicationAssembly);

        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(VahedScopeBehavior<,>));
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));

        // Not a pipeline behavior and not a repository: a piece of write-side domain logic shared
        // by the three «ارتباط معین با گروه تفصیلی» handlers, which keeps TB_ACCOUNT_LINK_LEVEL in
        // step with TB_ACCOUNT_LINK_TAFSILGROUP. Scoped, so it joins the caller's unit of work.
        services.AddScoped<AccountLevelLinkSynchronizer>();

        return services;
    }
}
