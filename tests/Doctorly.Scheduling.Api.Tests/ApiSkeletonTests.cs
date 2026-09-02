using System.Net;
using System.Text.Json;

namespace Doctorly.Scheduling.Api.Tests;

public sealed class ApiSkeletonTests(SchedulingApiFactory factory)
    : IClassFixture<SchedulingApiFactory>
{
    [Fact]
    public async Task Health_endpoint_reports_healthy()
    {
        using var client = factory.CreateClient();

        var response = await client.GetAsync(new Uri("/health", UriKind.Relative));

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        (await response.Content.ReadAsStringAsync()).ShouldBe("Healthy");
    }

    [Theory]
    [InlineData("public")]
    [InlineData("internal")]
    public async Task Both_openapi_documents_are_generated(string documentName)
    {
        using var client = factory.CreateClient();

        var response = await client.GetAsync(new Uri($"/swagger/{documentName}/swagger.json", UriKind.Relative));

        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        document.RootElement.GetProperty("openapi").GetString().ShouldNotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task Swagger_ui_is_served_from_the_root()
    {
        using var client = factory.CreateClient();

        var response = await client.GetAsync(new Uri("/index.html", UriKind.Relative));

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Unknown_route_returns_problem_details()
    {
        using var client = factory.CreateClient();

        var response = await client.GetAsync(new Uri("/api/does-not-exist", UriKind.Relative));

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        response.Content.Headers.ContentType?.MediaType.ShouldBe("application/problem+json");
    }
}
