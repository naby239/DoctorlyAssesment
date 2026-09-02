using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace Doctorly.Scheduling.Api.Tests;

public sealed class CancelEventTests(SchedulingApiFactory factory)
    : IClassFixture<SchedulingApiFactory>
{
    private static readonly Uri EventsUri = new("/api/events", UriKind.Relative);

    private static async Task<(Guid Id, Guid Version)> CreateEventAsync(HttpClient client, string title)
    {
        var response = await client.PostAsJsonAsync(EventsUri, new
        {
            title,
            startTime = "2026-07-01T09:00:00Z",
            endTime = "2026-07-01T09:30:00Z",
            attendees = new[]
            {
                new { name = "Anna Weber", email = $"{Guid.NewGuid():N}@practice.de" },
            },
        });

        response.StatusCode.ShouldBe(HttpStatusCode.Created);

        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());

        return (
            document.RootElement.GetProperty("id").GetGuid(),
            document.RootElement.GetProperty("version").GetGuid());
    }

    private static HttpRequestMessage CancelRequest(Guid id, Guid? ifMatch, string? reason = null)
    {
        var uri = reason is null
            ? $"/api/events/{id}"
            : $"/api/events/{id}?reason={Uri.EscapeDataString(reason)}";

        var request = new HttpRequestMessage(HttpMethod.Delete, new Uri(uri, UriKind.Relative));

        if (ifMatch.HasValue)
        {
            request.Headers.IfMatch.Add(new EntityTagHeaderValue($"\"{ifMatch.Value}\""));
        }

        return request;
    }

    [Fact]
    public async Task Cancelling_records_the_reason_and_keeps_the_attendees()
    {
        using var client = factory.CreateClient();
        var (id, version) = await CreateEventAsync(client, "Consultation");

        using var request = CancelRequest(id, version, "Patient called to cancel");
        var response = await client.SendAsync(request);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var root = document.RootElement;

        root.GetProperty("status").GetString().ShouldBe("Cancelled");
        root.GetProperty("cancellationReason").GetString().ShouldBe("Patient called to cancel");
        root.GetProperty("cancelledAt").GetDateTime().ShouldNotBe(default);
        root.GetProperty("attendees").GetArrayLength().ShouldBe(1);
    }

    [Fact]
    public async Task A_cancelled_event_is_still_retrievable()
    {
        using var client = factory.CreateClient();
        var (id, version) = await CreateEventAsync(client, "Consultation");

        using var request = CancelRequest(id, version);
        await client.SendAsync(request);

        var response = await client.GetAsync(new Uri($"/api/events/{id}", UriKind.Relative));

        // Not a 404 - the record is preserved, only its state changed.
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task A_cancelled_event_is_returned_by_the_cancelled_filter()
    {
        using var client = factory.CreateClient();
        var title = $"Cancelled {Guid.NewGuid():N}";
        var (id, version) = await CreateEventAsync(client, title);

        using var request = CancelRequest(id, version);
        await client.SendAsync(request);

        var listed = await client.GetFromJsonAsync<JsonElement>(
            new Uri($"/api/events?status=Cancelled&search={title}", UriKind.Relative));

        listed.GetProperty("totalCount").GetInt32().ShouldBe(1);
    }

    [Fact]
    public async Task A_cancelled_event_is_excluded_from_the_scheduled_filter()
    {
        using var client = factory.CreateClient();
        var title = $"Scheduled {Guid.NewGuid():N}";
        var (id, version) = await CreateEventAsync(client, title);

        using var request = CancelRequest(id, version);
        await client.SendAsync(request);

        var listed = await client.GetFromJsonAsync<JsonElement>(
            new Uri($"/api/events?status=Scheduled&search={title}", UriKind.Relative));

        listed.GetProperty("totalCount").GetInt32().ShouldBe(0);
    }

    [Fact]
    public async Task An_event_cannot_be_cancelled_twice()
    {
        using var client = factory.CreateClient();
        var (id, version) = await CreateEventAsync(client, "Consultation");

        using var first = CancelRequest(id, version);
        var cancelled = await first.SendAndReadVersionAsync(client);

        using var second = CancelRequest(id, cancelled);
        var response = await client.SendAsync(second);

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task A_cancelled_event_cannot_be_updated()
    {
        using var client = factory.CreateClient();
        var (id, version) = await CreateEventAsync(client, "Consultation");

        using var cancel = CancelRequest(id, version);
        var cancelledVersion = await cancel.SendAndReadVersionAsync(client);

        using var update = new HttpRequestMessage(
            HttpMethod.Put,
            new Uri($"/api/events/{id}", UriKind.Relative))
        {
            Content = JsonContent.Create(new
            {
                title = "Reopened",
                startTime = "2026-07-01T11:00:00Z",
                endTime = "2026-07-01T11:30:00Z",
            }),
        };

        update.Headers.IfMatch.Add(new EntityTagHeaderValue($"\"{cancelledVersion}\""));

        (await client.SendAsync(update)).StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Cancelling_without_if_match_is_refused()
    {
        using var client = factory.CreateClient();
        var (id, _) = await CreateEventAsync(client, "Consultation");

        using var request = CancelRequest(id, ifMatch: null);

        (await client.SendAsync(request)).StatusCode.ShouldBe(HttpStatusCode.PreconditionRequired);
    }

    [Fact]
    public async Task Cancelling_with_a_superseded_version_is_refused()
    {
        using var client = factory.CreateClient();
        var (id, _) = await CreateEventAsync(client, "Consultation");

        using var request = CancelRequest(id, Guid.NewGuid());

        (await client.SendAsync(request)).StatusCode.ShouldBe(HttpStatusCode.PreconditionFailed);
    }

    [Fact]
    public async Task Cancelling_an_unknown_event_is_not_found()
    {
        using var client = factory.CreateClient();

        using var request = CancelRequest(Guid.NewGuid(), Guid.NewGuid());

        (await client.SendAsync(request)).StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }
}

internal static class CancelRequestExtensions
{
    internal static async Task<Guid> SendAndReadVersionAsync(
        this HttpRequestMessage request,
        HttpClient client)
    {
        var response = await client.SendAsync(request);
        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());

        return document.RootElement.GetProperty("version").GetGuid();
    }
}
