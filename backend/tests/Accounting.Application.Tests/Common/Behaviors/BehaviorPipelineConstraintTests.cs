using Accounting.Application.Common.Behaviors;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.DependencyInjection;

namespace Accounting.Application.Tests.Common.Behaviors;

/// <summary>
/// Guards the single mistake that silently disabled <see cref="ValidationBehavior{TRequest,TResponse}"/>
/// for the entire write side of this application from phase 8 to phase 30.
///
/// <para>
/// <b>The bug.</b> A MediatR pipeline behavior declared
/// <c>where TRequest : IRequest&lt;TResponse&gt;</c> cannot be closed over a void command — one
/// declared <c>: IRequest</c> with no generic response, which under MediatR 14.2.0 does
/// <b>not</b> implement <c>IRequest&lt;Unit&gt;</c>. The .NET DI container skips such an
/// open-generic registration without throwing or logging anything. Every <c>Update</c>,
/// <c>Delete</c> and batch command in this project has that shape, so their FluentValidation
/// validators — written, registered and unit-tested — were never invoked by the pipeline.
/// </para>
///
/// <para>
/// <b>Why a convention test and not only the two pipeline probes below.</b> The probes prove the
/// two behaviors that exist today are wired correctly. This test additionally catches a
/// <i>future</i> behavior added with the same constraint, which would fail exactly as silently.
/// It is the same reasoning as <c>LegacyEnumMappingConventionTests</c>: the rule is easy to state
/// and impossible to remember at the moment it matters.
/// </para>
/// </summary>
public sealed class BehaviorPipelineConstraintTests
{
    [Fact]
    public void NoPipelineBehavior_ConstrainsTRequest_ToIRequest()
    {
        var behaviors = typeof(ValidationBehavior<,>).Assembly
            .GetTypes()
            .Where(type => type is { IsClass: true, IsAbstract: false, IsGenericTypeDefinition: true })
            .Where(type => type.GetInterfaces().Any(i =>
                i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IPipelineBehavior<,>)))
            .ToList();

        // If this trips, the discovery query above stopped matching — fix the query, do not
        // delete the test; an empty set would make every assertion below vacuously pass.
        Assert.NotEmpty(behaviors);

        var offenders = behaviors
            .Where(behavior => behavior
                .GetGenericArguments()[0]
                .GetGenericParameterConstraints()
                .Any(constraint =>
                    constraint == typeof(IRequest) ||
                    (constraint.IsGenericType &&
                     constraint.GetGenericTypeDefinition() == typeof(IRequest<>))))
            .Select(behavior => behavior.Name)
            .ToList();

        Assert.True(
            offenders.Count == 0,
            $"These pipeline behaviors constrain TRequest to IRequest/IRequest<TResponse>: " +
            $"{string.Join(", ", offenders)}. Under MediatR 14 that constraint cannot be satisfied " +
            "by a void command (': IRequest'), so the DI container skips the behavior for every " +
            "Update/Delete/batch command in this project — silently, with no exception and no log. " +
            "Use 'where TRequest : notnull' instead. See DependencyInjection.cs XML doc.");
    }

    // ----------------------------------------------------------------------------------------
    // End-to-end proof through a real MediatR + DI container, built the way Program.cs builds it.
    // The direct behavior.Handle(...) tests in ValidationBehaviorTests can never catch this class
    // of bug: they close the generics by hand, which is precisely the step the DI container was
    // failing to do.
    // ----------------------------------------------------------------------------------------

    public sealed record VoidCommand(string Value) : IRequest;

    public sealed class VoidCommandHandler : IRequestHandler<VoidCommand>
    {
        public static bool WasInvoked;

        public Task Handle(VoidCommand request, CancellationToken cancellationToken)
        {
            WasInvoked = true;
            return Task.CompletedTask;
        }
    }

    public sealed class VoidCommandValidator : AbstractValidator<VoidCommand>
    {
        public VoidCommandValidator() => RuleFor(c => c.Value).NotEmpty();
    }

    public sealed record ResponseCommand(string Value) : IRequest<string>;

    public sealed class ResponseCommandHandler : IRequestHandler<ResponseCommand, string>
    {
        public Task<string> Handle(ResponseCommand request, CancellationToken cancellationToken)
            => Task.FromResult("handled");
    }

    public sealed class ResponseCommandValidator : AbstractValidator<ResponseCommand>
    {
        public ResponseCommandValidator() => RuleFor(c => c.Value).NotEmpty();
    }

    private static IServiceProvider BuildRealMediatorContainer()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddMediatR(cfg =>
            cfg.RegisterServicesFromAssembly(typeof(BehaviorPipelineConstraintTests).Assembly));
        services.AddScoped<IValidator<VoidCommand>, VoidCommandValidator>();
        services.AddScoped<IValidator<ResponseCommand>, ResponseCommandValidator>();
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));

        return services.BuildServiceProvider();
    }

    [Fact]
    public async Task RealMediatorPipeline_VoidCommand_IsValidated()
    {
        // The regression test proper. This is the shape of every Update/Delete command in the
        // project, and it is the one that passed validation-free for 22 phases.
        var mediator = BuildRealMediatorContainer().GetRequiredService<IMediator>();
        VoidCommandHandler.WasInvoked = false;

        await Assert.ThrowsAsync<ValidationException>(
            () => mediator.Send(new VoidCommand(string.Empty)));

        Assert.False(VoidCommandHandler.WasInvoked);
    }

    [Fact]
    public async Task RealMediatorPipeline_VoidCommand_ValidInput_ReachesHandler()
    {
        // The other half: the fix must not turn validation into a blanket rejection.
        var mediator = BuildRealMediatorContainer().GetRequiredService<IMediator>();
        VoidCommandHandler.WasInvoked = false;

        await mediator.Send(new VoidCommand("something"));

        Assert.True(VoidCommandHandler.WasInvoked);
    }

    [Fact]
    public async Task RealMediatorPipeline_ResponseBearingCommand_IsStillValidated()
    {
        // This shape always worked; pinned so a future "fix" cannot trade one shape for the other.
        var mediator = BuildRealMediatorContainer().GetRequiredService<IMediator>();

        await Assert.ThrowsAsync<ValidationException>(
            () => mediator.Send(new ResponseCommand(string.Empty)));
    }
}
