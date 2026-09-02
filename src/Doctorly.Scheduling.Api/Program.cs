using Doctorly.Scheduling.Api.ErrorHandling;
using Doctorly.Scheduling.Api.OpenApi;
using Doctorly.Scheduling.Application;
using Doctorly.Scheduling.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

builder.Services.AddSchedulingOpenApi();

builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();

builder.Services.AddHealthChecks();

var app = builder.Build();

app.UseExceptionHandler();
app.UseStatusCodePages();

app.UseSchedulingOpenApi();
app.UseHttpsRedirection();

app.MapControllers();
app.MapHealthChecks("/health").AllowAnonymous();

await app.RunAsync().ConfigureAwait(false);

/// <summary>
/// Exposed so <c>WebApplicationFactory</c> can boot the API in integration tests.
/// </summary>
public partial class Program;
