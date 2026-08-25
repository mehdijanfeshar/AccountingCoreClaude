using Accounting.Api;
using Accounting.Api.Security;
using Accounting.Application;
using Accounting.Application.Common.Interfaces;
using Accounting.Infrastructure;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Tamin.Framework.Common.Security;

var builder = WebApplication.CreateBuilder(args);

if (builder.Environment.IsDevelopment())
{
    builder.Configuration.AddUserSecrets<Accounting.Infrastructure.AssemblyMarker>();
}

// Add services to the container.

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    // Surfaces the XML <summary>/<param> docs written on the controllers and their response
    // records (e.g. CreateAccountCodeResponse) in Swagger UI. Requires
    // <GenerateDocumentationFile> in Accounting.Api.csproj.
    var apiXmlFile = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var apiXmlPath = Path.Combine(AppContext.BaseDirectory, apiXmlFile);
    if (File.Exists(apiXmlPath))
    {
        options.IncludeXmlComments(apiXmlPath);
    }

    // Also surfaces the XML docs written on the Commands/Queries/DTOs that live in
    // Accounting.Application (CreateAccountCodeCommand, AccountCodeDto, VoucherHeadDto,
    // CreateVoucherDetailCommand, VoucherDetailDto, CreateVoucherHeadDetailInput, ...) — those
    // types are what the request/response bodies are actually made of, so without this,
    // Swagger UI would render every request/response schema with property names only, no
    // descriptions. Requires <GenerateDocumentationFile> in Accounting.Application.csproj.
    var applicationXmlFile = $"{typeof(Accounting.Application.DependencyInjection).Assembly.GetName().Name}.xml";
    var applicationXmlPath = Path.Combine(AppContext.BaseDirectory, applicationXmlFile);
    if (File.Exists(applicationXmlPath))
    {
        options.IncludeXmlComments(applicationXmlPath);
    }
});

builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

// --- Authentication (Tamin org IDP, JWT Bearer) ------------------------------------------------
//
// AddTaminJWTToken registers the standard "Bearer" scheme (JwtBearerHandler) with a statically
// embedded IssuerSigningKey for the given environment — no Authority/discovery/JWKS fetch at
// startup or per-request. It also sets AuthenticationOptions.DefaultScheme = "Bearer", so the
// fallback authorization policy below (SetFallbackPolicy) always has a scheme to authenticate
// and challenge with. Do NOT add a second AddAuthentication(...) call here — that would compete
// with the scheme this package already registers.
//
// The audience below is TEMPORARILY SHARED with the Financial_Account project, at the project
// owner's explicit instruction, pending a dedicated audience being registered for this
// Accounting service in the org IDP. Override via configuration key "Tamin:Idp:Audience" (User
// Secrets in Development, real config/secret store in other environments) once a dedicated
// audience exists — see the empty placeholder in appsettings.json.
builder.Services.AddTaminJWTToken(
    validAudience: builder.Configuration["Tamin:Idp:Audience"] ?? "136f697b158116450170417a5105224e",
    environment: builder.Environment.IsProduction()
        ? Tamin.Framework.Common.Security.Environments.Production
        : Tamin.Framework.Common.Security.Environments.Test);

// AddTaminJWTToken pre-wires JwtBearerEvents.OnMessageReceived and OnAuthenticationFailed (its
// own diagnostic logging) on the "Bearer" scheme it just registered. Assigning a brand-new
// `options.Events = new JwtBearerEvents { ... }` here would silently replace that whole events
// object and destroy those two delegates — so we CHAIN instead of replace: capture whatever is
// already on OnChallenge/OnForbidden and call it first, then apply our ProblemDetails shaping
// only if it is still safe to write a response. OnAuthenticationFailed, OnTokenValidated and
// OnMessageReceived are left completely untouched, exactly as the package configured them.
//
// (OnChallenge/OnForbidden themselves are not actually assigned by AddTaminJWTToken — they stay
// at the harmless JwtBearerEvents class defaults, `context => Task.CompletedTask`, which never
// call HandleResponse or touch the response. Verified by decompiling the installed
// Tamin.Framework.Common.Security 1.0.9 package. Chaining is still the correct, forward-safe
// pattern in case a future package version starts wiring these two itself.)
builder.Services.Configure<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme, options =>
{
    options.Events ??= new JwtBearerEvents();
    var events = options.Events;

    var originalOnChallenge = events.OnChallenge;
    var originalOnForbidden = events.OnForbidden;

    var isDevelopment = builder.Environment.IsDevelopment();

    // 401/403 are produced by this middleware and never reach GlobalExceptionHandler (they
    // aren't exceptions). Shape them explicitly here so they stay consistent with its
    // application/problem+json shape (traceId + generic Detail, no leaked specifics outside
    // Development).
    events.OnChallenge = async context =>
    {
        await originalOnChallenge(context);

        // Never write twice: skip if the original handler already produced a response.
        if (context.HttpContext.Response.HasStarted || context.Handled)
        {
            return;
        }

        context.HandleResponse();
        await WriteProblemDetailsAsync(
            context.HttpContext,
            StatusCodes.Status401Unauthorized,
            "Unauthorized",
            "Authentication is required to access this resource.",
            isDevelopment ? context.ErrorDescription : null);
    };

    events.OnForbidden = async context =>
    {
        await originalOnForbidden(context);

        if (context.HttpContext.Response.HasStarted)
        {
            return;
        }

        await WriteProblemDetailsAsync(
            context.HttpContext,
            StatusCodes.Status403Forbidden,
            "Forbidden",
            "You do not have permission to access this resource.",
            null);
    };
});

builder.Services.AddAuthorizationBuilder()
    .SetFallbackPolicy(new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build());

builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUser, HttpContextCurrentUser>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseExceptionHandler();

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();

// Mirrors GlobalExceptionHandler's ProblemDetails shape for the 401/403 responses produced by
// the authentication/authorization middleware, which run before MVC's exception handler is
// reachable.
static async Task WriteProblemDetailsAsync(
    HttpContext httpContext,
    int statusCode,
    string title,
    string detail,
    string? debugDetail)
{
    var problemDetails = new ProblemDetails
    {
        Status = statusCode,
        Title = title,
        Detail = detail,
        Instance = httpContext.Request.Path,
    };

    problemDetails.Extensions["traceId"] = httpContext.TraceIdentifier;

    if (!string.IsNullOrEmpty(debugDetail))
    {
        problemDetails.Extensions["debugDetail"] = debugDetail;
    }

    httpContext.Response.StatusCode = statusCode;

    // WriteAsJsonAsync unconditionally overwrites Response.ContentType with its own default
    // ("application/json; charset=utf-8") unless a contentType is passed explicitly here — so
    // the type must be supplied via the parameter, not set separately beforehand.
    await httpContext.Response.WriteAsJsonAsync(problemDetails, options: null, contentType: "application/problem+json");
}
