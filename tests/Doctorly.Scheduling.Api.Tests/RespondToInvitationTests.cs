using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace Doctorly.Scheduling.Api.Tests;

public sealed class RespondToInvitationTests(SchedulingApiFactory factory)
    : IClassFixture<SchedulingApiFactory>
{
    private static readonly Uri EventsUri = new("/api/events", UriKind.Relative);

    private sealed record CreatedEvent(Guid Id, Guid Version, Guid FirstAttendee, Guid SecondAttendee);

    private static async Task<CreatedEvent> CreateEventAsync(HttpClient client)
    {
        var response = await client.PostAsJsonAsync(EventsUri, new
        {
            title = "Consultation",
            startTime = "2026-08-03T09:00:00Z",
            endTime = "2026-08-03T09:30:00Z",
            attendees = new[]
            {
                new { name = "Anna Weber", email = $"{Guid.NewGuid():N}@practice.de" },
                new { name = "Dr Klein", email = $"{Guid.NewGuid():N}@practice.de" },
            },
        });

        response.StatusCode.ShouldBe(HttpStatusCode.Created);

        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var root = document.RootElement;
        var attendees = root.GetProperty("attendees");

        return new CreatedEvent(
            root.GetProperty("id").GetGuid(),
            root.GetProperty("version").GetGuid(),
            attendees[0].GetProperty("attendeeId").GetGuid(),
            attendees[1].GetProperty("attendeeId").GetGuid());
    }

    private static Task<HttpResponseMessage> RespondAsync(
        HttpClient client,
        Guid eventId,
        Guid attendeeId,
        string response) =>
        client.PostAsJsonAsync(
            new Uri($"/api/events/{eventId}/attendees/{attendeeId}/response", UriKind.Relative),
            new { response });

    private static async Task<string> StatusOfAsync(
        HttpResponseMessage response,
        Guid attendeeId)
    {
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());

        return document.RootElement.GetProperty("attendees")
            .EnumerateArray()
            .Single(a => a.GetProperty("attendeeId").GetGuid() == attendeeId)
            .GetProperty("status")
            .GetString()!;
    }

    [Theory]
    [InlineData("Accepted")]
    [InlineData("Declined")]
    public async Task An_attendee_can_answer_their_invitation(string answer)
    {
        using var client = factory.CreateClient();
        var created = await CreateEventAsync(client);

        var response = await RespondAsync(client, created.Id, created.FirstAttendee, answer);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        (await StatusOfAsync(response, created.FirstAttendee)).ShouldBe(answer);
    }

    [Fact]
    public async Task One_attendee_answering_leaves_the_others_pending()
    {
        using var client = factory.CreateClient();
        var created = await CreateEventAsync(client);

        var response = await RespondAsync(client, created.Id, created.FirstAttendee, "Accepted");

        (await StatusOfAsync(response, created.SecondAttendee)).ShouldBe("Pending");
    }

    [Fact]
    public async Task An_attendee_can_change_their_answer()
    {
        using var client = factory.CreateClient();
        var created = await CreateEventAsync(client);

        await RespondAsync(client, created.Id, created.FirstAttendee, "Accepted");
        var response = await RespondAsync(client, created.Id, created.FirstAttendee, "Declined");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        (await StatusOfAsync(response, created.FirstAttendee)).ShouldBe("Declined");
    }

    [Fact]
    public async Task Answering_records_when_the_reply_arrived()
    {
        using var client = factory.CreateClient();
        var created = await CreateEventAsync(client);

        var response = await RespondAsync(client, created.Id, created.FirstAttendee, "Accepted");

        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var invitation = document.RootElement.GetProperty("attendees")
            .EnumerateArray()
            .Single(a => a.GetProperty("attendeeId").GetGuid() == created.FirstAttendee);

        invitation.GetProperty("status").GetString().ShouldBe("Accepted");
    }

    [Theory]
    [InlineData("Pending")]
    [InlineData("Maybe")]
    public async Task An_answer_that_is_not_accept_or_decline_is_rejected(string answer)
    {
        using var client = factory.CreateClient();
        var created = await CreateEventAsync(client);

        var response = await RespondAsync(client, created.Id, created.FirstAttendee, answer);

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Someone_who_was_not_invited_has_no_invitation_to_answer()
    {
        using var client = factory.CreateClient();
        var created = await CreateEventAsync(client);

        var response = await RespondAsync(client, created.Id, Guid.NewGuid(), "Accepted");

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Answering_for_an_unknown_event_is_not_found()
    {
        using var client = factory.CreateClient();
        var created = await CreateEventAsync(client);

        var response = await RespondAsync(client, Guid.NewGuid(), created.FirstAttendee, "Accepted");

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task A_cancelled_event_no_longer_accepts_answers()
    {
        using var client = factory.CreateClient();
        var created = await CreateEventAsync(client);

        using var cancel = new HttpRequestMessage(
            HttpMethod.Delete,
            new Uri($"/api/events/{created.Id}", UriKind.Relative));

        cancel.Headers.IfMatch.Add(new EntityTagHeaderValue($"\"{created.Version}\""));
        (await client.SendAsync(cancel)).StatusCode.ShouldBe(HttpStatusCode.OK);

        var response = await RespondAsync(client, created.Id, created.FirstAttendee, "Accepted");

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Answering_moves_the_version_on()
    {
        using var client = factory.CreateClient();
        var created = await CreateEventAsync(client);

        var response = await RespondAsync(client, created.Id, created.FirstAttendee, "Accepted");

        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());

        // The representation changed, so the ETag has to change with it.
        document.RootElement.GetProperty("version").GetGuid().ShouldNotBe(created.Version);
    }
}
