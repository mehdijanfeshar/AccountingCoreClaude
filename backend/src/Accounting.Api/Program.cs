using Accounting.Api;
using Accounting.Application;
using Accounting.Infrastructure;

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
    // Surfaces the XML <summary>/<param> docs already written on the controllers and their
    // response records (e.g. CreateAccountCodeResponse) in Swagger UI. Requires
    // <GenerateDocumentationFile> in Accounting.Api.csproj.
    //
    // NOTE: this only covers types declared in Accounting.Api itself. The richer XML docs on
    // the Commands/Queries/DTOs (CreateAccountCodeCommand, AccountCodeDto, VoucherHeadDto, ...)
    // live in Accounting.Application, which does not yet generate its own XML doc file — those
    // request/response schemas will still show up in Swagger with property names only, no
    // descriptions, until Accounting.Application.csproj also sets
    // <GenerateDocumentationFile> and this call is extended with its XML path.
    var xmlFile = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    if (File.Exists(xmlPath))
    {
        options.IncludeXmlComments(xmlPath);
    }
});

builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseExceptionHandler();

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
