# Doctorly Scheduling API

## Prerequisites

The .NET 9 SDK. Nothing else — no database to install, no Docker, no accounts.

## Running the API

```bash
dotnet run --project src/Doctorly.Scheduling.Api
```

Open **https://localhost:7186**. The root URL serves Swagger UI, and every endpoint can be called
from there.

On first run the API creates a SQLite database file (`doctorly-scheduling.db`) next to the project
and applies its migrations. Delete that file to start again from empty.

To avoid the development HTTPS certificate — when using curl, for example — run the HTTP profile
instead and use `http://localhost:5267`:

```bash
dotnet run --project src/Doctorly.Scheduling.Api --launch-profile http
```

## Running the tests

```bash
dotnet test
```

## Example request

Against the HTTP profile above:

```bash
curl -X POST http://localhost:5267/api/events -H 'Content-Type: application/json' -d '{"title":"Consultation","description":"Annual check-up","startTime":"2026-03-02T09:00:00Z","endTime":"2026-03-02T09:30:00Z","attendees":[{"name":"Anna Weber","email":"anna.weber@practice.de"}]}'
```

Ready-made requests for every endpoint are in `src/Doctorly.Scheduling.Api/HTTP/`, and run directly
in Rider and Visual Studio.
