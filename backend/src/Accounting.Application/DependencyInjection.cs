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
    /// in this assembly, and the <see cref="ValidationBehavior{TRequest,TResponse}"/>
    /// pipeline behavior that runs them before every request reaches its handler.
    /// </summary>
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        var applicationAssembly = Assembly.GetExecutingAssembly();

        services.AddMediatR(configuration => configuration.RegisterServicesFromAssembly(applicationAssembly));

        services.AddValidatorsFromAssembly(applicationAssembly);

        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));

        return services;
    }
}
