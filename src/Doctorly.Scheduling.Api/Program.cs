using System.Text.Json.Serialization;
using Doctorly.Scheduling.Api.ErrorHandling;
using Doctorly.Scheduling.Api.OpenApi;
using Doctorly.Scheduling.Api.Serialization;
using Doctorly.Scheduling.Application;
using Doctorly.Scheduling.Infrastructure;
using Doctorly.Scheduling.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers(options =>
        options.ModelBinderProviders.Insert(0, new RequiredOffsetDateTimeModelBinderProvider()))
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new RequiredOffsetDateTimeConverter());

        // Enums travel as their names, so a caller sends "Accepted" rather than 1.
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    });

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

builder.Services.AddSchedulingOpenApi();

builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();

builder.Services.AddHealthChecks();

var app = builder.Build();

// Creates and migrates the SQLite file on first run so the project works from a clean clone.
await using (var scope = app.Services.CreateAsyncScope())
{
    await scope.ServiceProvider.GetRequiredService<SchedulingDbContext>()
        .Database.MigrateAsync().ConfigureAwait(false);
}

app.UseExceptionHandler();
app.UseStatusCodePages();

app.UseSchedulingOpenApi();
app.UseHttpsRedirection();

app.MapControllers();
app.MapHealthChecks("/health").AllowAnonymous();

await app.RunAsync().ConfigureAwait(false);

// Exposed for WebApplicationFactory in the integration tests.
public partial class Program;
