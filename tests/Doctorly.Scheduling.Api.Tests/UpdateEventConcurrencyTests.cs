using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace Doctorly.Scheduling.Api.Tests;

public sealed class UpdateEventConcurrencyTests(SchedulingApiFactory factory)
    : IClassFixture<SchedulingApiFactory>
{
    private static readonly Uri EventsUri = new("/api/events", UriKind.Relative);

    private static object AnUpdate(string title) => new
    {
        title,
        description = "Updated",
        startTime = "2026-05-05T10:00:00Z",
        endTime = "2026-05-05T10:30:00Z",
    };

    private async Task<(Guid Id, Guid Version)> CreateEventAsync(HttpClient client)
    {
        var response = await client.PostAsJsonAsync(EventsUri, new
        {
            title = "Consultation",
            startTime = "2026-05-05T09:00:00Z",
            endTime = "2026-05-05T09:30:00Z",
        });

        response.StatusCode.ShouldBe(HttpStatusCode.Created);

        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());

        return (
            document.RootElement.GetProperty("id").GetGuid(),
            document.RootElement.GetProperty("version").GetGuid());
    }

    private static HttpRequestMessage UpdateRequest(Guid id, Guid? ifMatch, string title)
    {
        var request = new HttpRequestMessage(HttpMethod.Put, new Uri($"/api/events/{id}", UriKind.Relative))
        {
            Content = JsonContent.Create(AnUpdate(title)),
        };

        if (ifMatch.HasValue)
        {
            request.Headers.IfMatch.Add(new EntityTagHeaderValue($"\"{ifMatch.Value}\""));
        }

        return request;
    }

    [Fact]
    public async Task An_update_carrying_the_current_version_succeeds_and_moves_the_version_on()
    {
        using var client = factory.CreateClient();
        var (id, version) = await CreateEventAsync(client);

        using var request = UpdateRequest(id, version, "Rescheduled");
        var response = await client.SendAsync(request);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        document.RootElement.GetProperty("version").GetGuid().ShouldNotBe(version);
        document.RootElement.GetProperty("title").GetString().ShouldBe("Rescheduled");
    }

    [Fact]
    public async Task An_update_without_if_match_is_refused()
    {
        using var client = factory.CreateClient();
        var (id, _) = await CreateEventAsync(client);

        using var request = UpdateRequest(id, ifMatch: null, "No header");
        var response = await client.SendAsync(request);

        response.StatusCode.ShouldBe(HttpStatusCode.PreconditionRequired);
    }

    [Fact]
    public async Task An_update_carrying_a_superseded_version_is_refused()
    {
        using var client = factory.CreateClient();
        var (id, original) = await CreateEventAsync(client);

        using var first = UpdateRequest(id, original, "First writer");
        (await client.SendAsync(first)).StatusCode.ShouldBe(HttpStatusCode.OK);

        // The second caller read before that write landed and is now working from a stale copy.
        using var second = UpdateRequest(id, original, "Second writer");
        var response = await client.SendAsync(second);

        response.StatusCode.ShouldBe(HttpStatusCode.PreconditionFailed);
    }

    [Fact]
    public async Task A_stale_update_leaves_the_earlier_change_intact()
    {
        using var client = factory.CreateClient();
        var (id, original) = await CreateEventAsync(client);

        using var first = UpdateRequest(id, original, "First writer");
        await client.SendAsync(first);

        using var second = UpdateRequest(id, original, "Second writer");
        await client.SendAsync(second);

        var current = await client.GetFromJsonAsync<JsonElement>(
            new Uri($"/api/events/{id}", UriKind.Relative));

        current.GetProperty("title").GetString().ShouldBe("First writer");
    }

    [Fact]
    public async Task Only_one_of_several_simultaneous_writers_wins()
    {
        using var client = factory.CreateClient();
        var (id, version) = await CreateEventAsync(client);

        var requests = Enumerable.Range(0, 5)
            .Select(i => UpdateRequest(id, version, $"Writer {i}"))
            .ToList();

        var responses = await Task.WhenAll(requests.Select(r => client.SendAsync(r)));

        try
        {
            responses.Count(r => r.StatusCode == HttpStatusCode.OK).ShouldBe(1);
            responses.Count(r => r.StatusCode == HttpStatusCode.PreconditionFailed).ShouldBe(4);
        }
        finally
        {
            foreach (var response in responses)
            {
                response.Dispose();
            }

            foreach (var request in requests)
            {
                request.Dispose();
            }
        }
    }

    [Fact]
    public async Task An_update_to_an_unknown_event_is_not_found()
    {
        using var client = factory.CreateClient();

        using var request = UpdateRequest(Guid.NewGuid(), Guid.NewGuid(), "Nobody");
        var response = await client.SendAsync(request);

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Times_read_back_from_the_database_still_state_they_are_utc()
    {
        using var client = factory.CreateClient();
        var (id, _) = await CreateEventAsync(client);

        var response = await client.GetAsync(new Uri($"/api/events/{id}", UriKind.Relative));
        var body = await response.Content.ReadAsStringAsync();

        // Without the 'Z' a client would read the appointment as local time.
        body.ShouldContain("2026-05-05T09:00:00Z");
    }
}
