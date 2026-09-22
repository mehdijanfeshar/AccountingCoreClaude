using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text.Encodings.Web;
using Accounting.Application.Common.Exceptions;
using Accounting.Application.Common.Interfaces;
using Accounting.Application.WorkShops.Queries;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Accounting.Api.Tests.Pipeline;

/// <summary>
/// The only tests in this project that drive a real HTTP request through the real application:
/// routing, authentication, the MediatR pipeline with both behaviors resolved by the real
/// <c>AddApplication()</c> registration, the handler, and <c>GlobalExceptionHandler</c>'s
/// ProblemDetails output.
///
/// <para>
/// <b>Why this exists.</b> Phases 31 and 32 both changed runtime behaviour — validation started
/// running on the write path for the first time since phase 8, and by-id lookups started refusing
/// other units' rows — and neither could be verified end to end. Every other test either
/// constructs a handler directly or mocks <c>IMediator</c>, which skips exactly the wiring both
/// changes depend on. Phase 31's own notes recorded that gap honestly; this closes it.
/// </para>
///
/// <para>
/// <b>No database, by design.</b> The only Oracle this project has is the organisation's live one,
/// and the standing rule is that tests never touch it. The repository is replaced with a stub, so
/// the DbContext is registered but never queried — everything under test sits above it.
/// </para>
/// </summary>
public sealed class RealHttpPipelineTests : IClassFixture<RealHttpPipelineTests.Factory>
{
    private const string CallerUnit = "0042";
    private const string OtherUnit = "0043";
    private static readonly Guid SomeId = Guid.Parse("22222222-2222-2222-2222-222222222222");

    private readonly Factory _factory;

    public RealHttpPipelineTests(Factory factory) => _factory = factory;

    // ---------------------------------------------------------------------------------------
    // Phase 32 — record ownership answers 403 over real HTTP
    // ---------------------------------------------------------------------------------------

    [Fact]
    public async Task GetById_ForAnotherUnitsRecord_Returns403WithProblemDetails()
    {
        _factory.ReadRepository.Behaviour = ReadStub.Mode.OtherUnit;
        var client = _factory.CreateClient();

        var response = await client.GetAsync($"/api/work-shops/{SomeId}");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);

        var problem = await response.Content.ReadFromJsonAsync<ProblemDetailsPayload>();
        Assert.Equal(403, problem!.Status);

        // The owning unit must never reach the client. It is in the exception message, which goes
        // to the log — VahedOwnershipTests holds that end; this asserts the other end.
        Assert.DoesNotContain(OtherUnit, problem.Detail ?? string.Empty, StringComparison.Ordinal);
        Assert.DoesNotContain(CallerUnit, problem.Detail ?? string.Empty, StringComparison.Ordinal);
    }

    [Fact]
    public async Task GetById_ForOwnUnitsRecord_Returns200()
    {
        // The other half: the guard must not reject everything. A test that only proves 403 would
        // still pass if the endpoint were broken outright.
        _factory.ReadRepository.Behaviour = ReadStub.Mode.OwnUnit;
        var client = _factory.CreateClient();

        var response = await client.GetAsync($"/api/work-shops/{SomeId}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task GetById_ForMissingRecord_Returns404_Not403()
    {
        // "Not found" and "not yours" must stay distinguishable: conflating them would undo the
        // reason 403 was chosen over 404 in the first place.
        _factory.ReadRepository.Behaviour = ReadStub.Mode.Missing;
        var client = _factory.CreateClient();

        var response = await client.GetAsync($"/api/work-shops/{SomeId}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    // ---------------------------------------------------------------------------------------
    // Phase 31 — validation really runs on a void command, through the real container
    // ---------------------------------------------------------------------------------------

    [Fact]
    public async Task Update_WithInvalidBody_Returns400_ProvingValidationRunsOnVoidCommands()
    {
        // UpdateWorkShopCommand is `: IRequest` — the shape whose validator the DI container
        // silently skipped from phase 8 to phase 30.
        //
        // ⚠️ The body below must bind cleanly and fail only FluentValidation, or this test proves
        // nothing. An earlier version sent isActive = 1 against a bool property; ASP.NET's own
        // model binding rejected it with 400 before MediatR was ever reached, so the test passed
        // even with the phase 31 bug deliberately put back. Every field here is well-typed and
        // present; what is wrong is accountCodeId = Guid.Empty (NotEqual rule) and a name past
        // MaximumLength(100) — violations only the validator knows about.
        //
        // With the bug present this request reaches the handler, which asks Oracle for a row and
        // fails there instead, so the expected status changes. That is what makes this assertion
        // load-bearing rather than decorative.
        var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync(
            $"/api/work-shops/{SomeId}/update",
            new
            {
                accountCodeId = Guid.Empty,
                branchId = (Guid?)null,
                workShopName = new string('x', 101),
                workShopCode = "001",
                isActive = true,
                checkFile = (byte[]?)null,
            });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    private sealed record ProblemDetailsPayload(int Status, string? Title, string? Detail);

    // ---------------------------------------------------------------------------------------

    /// <summary>
    /// Boots the real <c>Program</c> with two substitutions and nothing else: a test
    /// authentication scheme (the real one would need a live IDP token) and a stubbed read
    /// repository (the real one would need Oracle). Everything between them is production wiring.
    /// </summary>
    public sealed class Factory : WebApplicationFactory<Program>
    {
        public ReadStub ReadRepository { get; } = new();

        protected override void ConfigureWebHost(Microsoft.AspNetCore.Hosting.IWebHostBuilder builder)
        {
            builder.UseSetting("ConnectionStrings:DefaultConnection", "Data Source=unused;User Id=unused;Password=unused;");

            builder.ConfigureServices(services =>
            {
                services.RemoveAll<IWorkShopReadRepository>();
                services.AddSingleton<IWorkShopReadRepository>(ReadRepository);

                services.AddAuthentication(TestAuthHandler.SchemeName)
                    .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>(TestAuthHandler.SchemeName, _ => { });
            });
        }
    }

    /// <summary>
    /// Stands in for the repository so the access decision can be driven without a database. It
    /// makes the same call into <c>VahedOwnership</c> the real repository makes, from the same
    /// place in the stack, which is the part these tests are about.
    /// </summary>
    public sealed class ReadStub : IWorkShopReadRepository
    {
        public enum Mode { OwnUnit, OtherUnit, Missing }

        public Mode Behaviour { get; set; } = Mode.OwnUnit;

        public Task<WorkShopDto?> GetByIdAsync(Guid id, string vahedCode, CancellationToken cancellationToken = default)
        {
            switch (Behaviour)
            {
                case Mode.Missing:
                    return Task.FromResult<WorkShopDto?>(null);

                case Mode.OtherUnit:
                    throw new UnitAccessDeniedException("WorkShop", id, OtherUnit, vahedCode);

                default:
                    return Task.FromResult<WorkShopDto?>(new WorkShopDto(
                        id, Guid.NewGuid(), null, "کارگاه", "001", vahedCode, true,
                        DateTime.UtcNow, null, "u1", null, false,
                        Array.Empty<WorkShopTafsiliLinkDto>()));
            }
        }

        public Task<Application.Common.PagedResult<WorkShopDto>> GetPagedAsync(
            int pageNumber,
            int pageSize,
            string vahedCode,
            CancellationToken cancellationToken = default)
            => Task.FromResult(new Application.Common.PagedResult<WorkShopDto>
            {
                Items = Array.Empty<WorkShopDto>(),
                PageNumber = pageNumber,
                PageSize = pageSize,
                TotalCount = 0,
            });
    }

    public sealed class TestAuthHandler : AuthenticationHandler<AuthenticationSchemeOptions>
    {
        public const string SchemeName = "Test";

        public TestAuthHandler(
            IOptionsMonitor<AuthenticationSchemeOptions> options,
            ILoggerFactory logger,
            UrlEncoder encoder)
            : base(options, logger, encoder)
        {
        }

        protected override Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            // The unit claim is what VahedScopeBehavior stamps onto every scoped request; without
            // it these tests would prove nothing about ownership.
            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, "tester"),
                new Claim("urn:tamin:jwt:claim:org", CallerUnit),
            };

            var principal = new ClaimsPrincipal(new ClaimsIdentity(claims, SchemeName));
            return Task.FromResult(AuthenticateResult.Success(new AuthenticationTicket(principal, SchemeName)));
        }
    }
}
