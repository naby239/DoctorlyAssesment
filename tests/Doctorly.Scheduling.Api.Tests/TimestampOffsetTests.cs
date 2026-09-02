using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace Doctorly.Scheduling.Api.Tests;

// A timestamp with no offset is read in the server's local time, which would make the stored
// instant depend on where the API is deployed. These pin the rejection down.
public sealed class TimestampOffsetTests(SchedulingApiFactory factory)
    : IClassFixture<SchedulingApiFactory>
{
    private static object AnEvent(string start, string end) => new
    {
        title = "Consultation",
        startTime = start,
        endTime = end,
    };

    private async Task<HttpResponseMessage> PostAsync(string start, string end)
    {
        using var client = factory.CreateClient();

        return await client.PostAsJsonAsync(
            new Uri("/api/events", UriKind.Relative),
            AnEvent(start, end));
    }

    [Theory]
    [InlineData("2026-03-02T09:00:00Z", "2026-03-02T10:00:00Z")]
    [InlineData("2026-03-02T09:00:00+02:00", "2026-03-02T10:00:00+02:00")]
    [InlineData("2026-03-02T09:00:00-05:00", "2026-03-02T10:00:00-05:00")]
    [InlineData("2026-03-02T09:00:00.500Z", "2026-03-02T10:00:00.500Z")]
    public async Task A_timestamp_that_states_its_offset_is_accepted(string start, string end)
    {
        var response = await PostAsync(start, end);

        response.StatusCode.ShouldBe(HttpStatusCode.Created);
    }

    [Theory]
    [InlineData("2026-03-02T09:00:00", "2026-03-02T10:00:00")]
    [InlineData("2026-03-02", "2026-03-03")]
    public async Task A_timestamp_without_an_offset_is_rejected(string start, string end)
    {
        var response = await PostAsync(start, end);

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        (await response.Content.ReadAsStringAsync()).ShouldContain("offset");
    }

    [Theory]
    [InlineData("from=2026-06-10T11:00:00Z&to=2026-06-10T13:00:00Z")]
    [InlineData("from=2026-06-10T13:00:00%2B02:00&to=2026-06-10T15:00:00%2B02:00")]
    [InlineData("pageSize=10")]
    public async Task A_query_filter_that_states_its_offset_is_accepted(string queryString)
    {
        using var client = factory.CreateClient();

        var response = await client.GetAsync(new Uri($"/api/events?{queryString}", UriKind.Relative));

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Theory]
    [InlineData("from=2026-06-10T11:00:00")]
    [InlineData("to=2026-06-10T13:00:00")]
    [InlineData("from=not-a-date")]
    public async Task A_query_filter_without_an_offset_is_rejected(string queryString)
    {
        using var client = factory.CreateClient();

        var response = await client.GetAsync(new Uri($"/api/events?{queryString}", UriKind.Relative));

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        (await response.Content.ReadAsStringAsync()).ShouldContain("offset");
    }

    [Fact]
    public async Task An_offset_is_converted_to_utc_on_the_way_in()
    {
        var response = await PostAsync("2026-03-02T09:00:00+02:00", "2026-03-02T10:00:00+02:00");

        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var stored = document.RootElement.GetProperty("startTime").GetDateTime();

        stored.ShouldBe(new DateTime(2026, 3, 2, 7, 0, 0, DateTimeKind.Utc));
    }
}
