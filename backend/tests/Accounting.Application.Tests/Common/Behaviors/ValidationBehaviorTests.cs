using Accounting.Application.Common.Behaviors;
using FluentValidation;
using FluentValidation.Results;
using MediatR;
using Moq;

namespace Accounting.Application.Tests.Common.Behaviors;

public sealed class ValidationBehaviorTests
{
    public sealed record FakeRequest(string Value) : IRequest<string>;

    [Fact]
    public async Task Handle_FailingValidator_ThrowsValidationException_AndNeverInvokesInnerHandler()
    {
        var failingValidator = new Mock<IValidator<FakeRequest>>();
        failingValidator
            .Setup(v => v.ValidateAsync(It.IsAny<ValidationContext<FakeRequest>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult(new[] { new ValidationFailure("Value", "Value is required") }));

        var behavior = new ValidationBehavior<FakeRequest, string>(new[] { failingValidator.Object });

        var innerHandlerInvoked = false;
        RequestHandlerDelegate<string> next = _ =>
        {
            innerHandlerInvoked = true;
            return Task.FromResult("handled");
        };

        await Assert.ThrowsAsync<ValidationException>(
            () => behavior.Handle(new FakeRequest(string.Empty), next, CancellationToken.None));

        Assert.False(innerHandlerInvoked);
    }

    [Fact]
    public async Task Handle_PassingValidator_InvokesInnerHandler_AndReturnsItsResult()
    {
        var passingValidator = new Mock<IValidator<FakeRequest>>();
        passingValidator
            .Setup(v => v.ValidateAsync(It.IsAny<ValidationContext<FakeRequest>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult());

        var behavior = new ValidationBehavior<FakeRequest, string>(new[] { passingValidator.Object });

        var innerHandlerInvoked = false;
        RequestHandlerDelegate<string> next = _ =>
        {
            innerHandlerInvoked = true;
            return Task.FromResult("handled");
        };

        var result = await behavior.Handle(new FakeRequest("ok"), next, CancellationToken.None);

        Assert.True(innerHandlerInvoked);
        Assert.Equal("handled", result);
    }

    [Fact]
    public async Task Handle_NoValidatorsRegistered_InvokesInnerHandler()
    {
        var behavior = new ValidationBehavior<FakeRequest, string>(Array.Empty<IValidator<FakeRequest>>());

        var innerHandlerInvoked = false;
        RequestHandlerDelegate<string> next = _ =>
        {
            innerHandlerInvoked = true;
            return Task.FromResult("handled");
        };

        var result = await behavior.Handle(new FakeRequest("ok"), next, CancellationToken.None);

        Assert.True(innerHandlerInvoked);
        Assert.Equal("handled", result);
    }
}
