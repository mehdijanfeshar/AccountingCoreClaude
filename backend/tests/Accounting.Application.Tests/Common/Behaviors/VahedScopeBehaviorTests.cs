using Accounting.Application.Common.Behaviors;
using Accounting.Application.Common.Exceptions;
using Accounting.Application.Common.Interfaces;
using Accounting.Application.Common.Security;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace Accounting.Application.Tests.Common.Behaviors;

public sealed class VahedScopeBehaviorTests
{
    /// <summary>A fake request that opts in to VahedCode scoping.</summary>
    public sealed record ScopedRequest(string ClientSuppliedVahedCode) : IRequest<string>, IVahedScopedCommand
    {
        public string VahedCode { get; set; } = string.Empty;
    }

    /// <summary>A fake request that does NOT implement <see cref="IVahedScoped"/>.</summary>
    public sealed record UnscopedRequest(string Value) : IRequest<string>;

    private static Mock<ICurrentUser> CurrentUserMock(string? vahedCode)
    {
        var currentUser = new Mock<ICurrentUser>();
        currentUser.SetupGet(u => u.VahedCode).Returns(vahedCode);
        return currentUser;
    }

    [Fact]
    public async Task Handle_ScopedRequest_OverwritesClientSuppliedVahedCode_WithCurrentUserValue()
    {
        var currentUser = CurrentUserMock("0007");
        var behavior = new VahedScopeBehavior<ScopedRequest, string>(currentUser.Object);
        var request = new ScopedRequest(ClientSuppliedVahedCode: "9999") { VahedCode = "9999" };

        RequestHandlerDelegate<string> next = _ => Task.FromResult("handled");

        await behavior.Handle(request, next, CancellationToken.None);

        Assert.Equal("0007", request.VahedCode);
    }

    [Fact]
    public async Task Handle_NullVahedCodeClaim_ThrowsMissingVahedScopeException_AndNeverInvokesNext()
    {
        var currentUser = CurrentUserMock(null);
        var behavior = new VahedScopeBehavior<ScopedRequest, string>(currentUser.Object);
        var request = new ScopedRequest(ClientSuppliedVahedCode: "0001");

        var nextInvoked = false;
        RequestHandlerDelegate<string> next = _ =>
        {
            nextInvoked = true;
            return Task.FromResult("handled");
        };

        await Assert.ThrowsAsync<MissingVahedScopeException>(
            () => behavior.Handle(request, next, CancellationToken.None));

        Assert.False(nextInvoked);
    }

    [Fact]
    public async Task Handle_EmptyVahedCodeClaim_ThrowsMissingVahedScopeException_AndNeverInvokesNext()
    {
        var currentUser = CurrentUserMock(string.Empty);
        var behavior = new VahedScopeBehavior<ScopedRequest, string>(currentUser.Object);
        var request = new ScopedRequest(ClientSuppliedVahedCode: "0001");

        var nextInvoked = false;
        RequestHandlerDelegate<string> next = _ =>
        {
            nextInvoked = true;
            return Task.FromResult("handled");
        };

        await Assert.ThrowsAsync<MissingVahedScopeException>(
            () => behavior.Handle(request, next, CancellationToken.None));

        Assert.False(nextInvoked);
    }

    [Fact]
    public async Task Handle_WhitespaceVahedCodeClaim_ThrowsMissingVahedScopeException_AndNeverInvokesNext()
    {
        var currentUser = CurrentUserMock("   ");
        var behavior = new VahedScopeBehavior<ScopedRequest, string>(currentUser.Object);
        var request = new ScopedRequest(ClientSuppliedVahedCode: "0001");

        var nextInvoked = false;
        RequestHandlerDelegate<string> next = _ =>
        {
            nextInvoked = true;
            return Task.FromResult("handled");
        };

        await Assert.ThrowsAsync<MissingVahedScopeException>(
            () => behavior.Handle(request, next, CancellationToken.None));

        Assert.False(nextInvoked);
    }

    [Fact]
    public async Task Handle_VahedCodeClaimLongerThanFourCharacters_ThrowsMissingVahedScopeException_NeverTruncates()
    {
        var currentUser = CurrentUserMock("00001");
        var behavior = new VahedScopeBehavior<ScopedRequest, string>(currentUser.Object);
        var request = new ScopedRequest(ClientSuppliedVahedCode: "0001");

        var nextInvoked = false;
        RequestHandlerDelegate<string> next = _ =>
        {
            nextInvoked = true;
            return Task.FromResult("handled");
        };

        await Assert.ThrowsAsync<MissingVahedScopeException>(
            () => behavior.Handle(request, next, CancellationToken.None));

        Assert.False(nextInvoked);
        // Never silently truncated to the first 4 characters.
        Assert.NotEqual("0000", request.VahedCode);
    }

    [Fact]
    public async Task Handle_VahedCodeClaimAtExactlyFourCharacters_Passes()
    {
        var currentUser = CurrentUserMock("0001");
        var behavior = new VahedScopeBehavior<ScopedRequest, string>(currentUser.Object);
        var request = new ScopedRequest(ClientSuppliedVahedCode: "9999");

        RequestHandlerDelegate<string> next = _ => Task.FromResult("handled");

        var result = await behavior.Handle(request, next, CancellationToken.None);

        Assert.Equal("0001", request.VahedCode);
        Assert.Equal("handled", result);
    }

    [Fact]
    public async Task Handle_RequestNotImplementingIVahedScoped_PassesThroughUntouched_AndInvokesNext()
    {
        // ICurrentUser.VahedCode is deliberately left unset/never verified here — this proves
        // the behavior does not even look at it for a request that did not opt in.
        var currentUser = new Mock<ICurrentUser>();
        var behavior = new VahedScopeBehavior<UnscopedRequest, string>(currentUser.Object);
        var request = new UnscopedRequest("ok");

        var nextInvoked = false;
        RequestHandlerDelegate<string> next = _ =>
        {
            nextInvoked = true;
            return Task.FromResult("handled");
        };

        var result = await behavior.Handle(request, next, CancellationToken.None);

        Assert.True(nextInvoked);
        Assert.Equal("handled", result);
        currentUser.VerifyGet(u => u.VahedCode, Times.Never);
    }

    // ------------------------------------------------------------------------------------------
    // Real-pipeline regression guard.
    //
    // Every test above calls behavior.Handle(...) directly, which never exercises how the .NET
    // DI container resolves the OPEN-GENERIC IPipelineBehavior<,> registration in
    // DependencyInjection.cs for a given closed TRequest/TResponse pair — and that resolution
    // step is exactly where this project already has one proven bug (ValidationBehavior silently
    // never running for void commands — see DependencyInjection.cs XML doc). These two tests
    // build a real MediatR + DI container, the same way Program.cs does, and prove
    // VahedScopeBehavior is actually invoked for BOTH a response-bearing request and a genuinely
    // void one (: IRequest, no TResponse — the exact shape of every Update/Delete command in this
    // project, e.g. UpdateWorkShopCommand). If VahedScopeBehavior's class declaration is ever
    // "cleaned up" to add back `where TRequest : IRequest<TResponse>`, the void-command test below
    // starts failing — that is the point.
    // ------------------------------------------------------------------------------------------

    public sealed record VoidScopedRequest : IRequest, IVahedScopedCommand
    {
        public string VahedCode { get; set; } = string.Empty;
    }

    public sealed class VoidScopedRequestHandler : IRequestHandler<VoidScopedRequest>
    {
        public static string? ObservedVahedCode;

        public Task Handle(VoidScopedRequest request, CancellationToken cancellationToken)
        {
            ObservedVahedCode = request.VahedCode;
            return Task.CompletedTask;
        }
    }

    public sealed class ScopedRequestHandler : IRequestHandler<ScopedRequest, string>
    {
        public Task<string> Handle(ScopedRequest request, CancellationToken cancellationToken)
            => Task.FromResult(request.VahedCode);
    }

    private static IServiceProvider BuildRealMediatorContainer(string currentUserVahedCode)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(VahedScopeBehaviorTests).Assembly));
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(VahedScopeBehavior<,>));

        var currentUser = new Mock<ICurrentUser>();
        currentUser.SetupGet(u => u.VahedCode).Returns(currentUserVahedCode);
        services.AddSingleton(currentUser.Object);

        return services.BuildServiceProvider();
    }

    [Fact]
    public async Task RealMediatorPipeline_VoidCommand_StillInvokesVahedScopeBehavior()
    {
        // This is the case that silently breaks if the class regains an
        // `IRequest<TResponse>` constraint: MediatR 14.2.0's void-request dispatch never
        // implements IRequest<Unit> for `: IRequest`-only requests, so a constrained open-generic
        // behavior registration is skipped by the DI container without error.
        var provider = BuildRealMediatorContainer(currentUserVahedCode: "0042");
        var mediator = provider.GetRequiredService<IMediator>();
        VoidScopedRequestHandler.ObservedVahedCode = null;

        await mediator.Send(new VoidScopedRequest { VahedCode = "9999" });

        Assert.Equal("0042", VoidScopedRequestHandler.ObservedVahedCode);
    }

    [Fact]
    public async Task RealMediatorPipeline_ResponseBearingCommand_InvokesVahedScopeBehavior()
    {
        var provider = BuildRealMediatorContainer(currentUserVahedCode: "0043");
        var mediator = provider.GetRequiredService<IMediator>();

        var result = await mediator.Send(new ScopedRequest(ClientSuppliedVahedCode: "9999") { VahedCode = "9999" });

        Assert.Equal("0043", result);
    }
}
