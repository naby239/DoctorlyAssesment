// Microsoft.OpenApi v2 (via Swashbuckle 10) moved these types out of Microsoft.OpenApi.Models.
using Microsoft.OpenApi;

namespace Doctorly.Scheduling.Api.OpenApi;

public static class SwaggerConfiguration
{
    public static IServiceCollection AddSchedulingOpenApi(this IServiceCollection services)
    {
        services.AddEndpointsApiExplorer();

        services.AddSwaggerGen(options =>
        {
            options.SwaggerDoc(ApiDocuments.Public, new OpenApiInfo
            {
                Title = "Doctorly Scheduling API",
                Version = "v1",
                Description =
                    "Practice scheduling: calendar events, attendees and invitation responses. "
                    + "Writes use optimistic concurrency via the ETag and If-Match headers.",
                Contact = new OpenApiContact { Name = "Doctorly GmbH", Url = new Uri("https://www.doctorly.de") },
            });

            options.SwaggerDoc(ApiDocuments.Internal, new OpenApiInfo
            {
                Title = "Doctorly Scheduling API (internal)",
                Version = "v1",
                Description = "Full surface including operational endpoints.",
            });

            options.DocInclusionPredicate((documentName, apiDescription) =>
                documentName == ApiDocuments.Internal
                || apiDescription.GroupName != ApiDocuments.Internal);

            var xmlFile = $"{typeof(SwaggerConfiguration).Assembly.GetName().Name}.xml";
            var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);

            if (File.Exists(xmlPath))
            {
                options.IncludeXmlComments(xmlPath);
            }

            options.SupportNonNullableReferenceTypes();
        });

        return services;
    }

    public static WebApplication UseSchedulingOpenApi(this WebApplication app)
    {
        app.UseSwagger();

        app.UseSwaggerUI(options =>
        {
            options.SwaggerEndpoint($"/swagger/{ApiDocuments.Public}/swagger.json", "Public API");
            options.SwaggerEndpoint($"/swagger/{ApiDocuments.Internal}/swagger.json", "Internal API");
            options.DocumentTitle = "Doctorly Scheduling API";
            options.RoutePrefix = string.Empty;
        });

        return app;
    }
}
