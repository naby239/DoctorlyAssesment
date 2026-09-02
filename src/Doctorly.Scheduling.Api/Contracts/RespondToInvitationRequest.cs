using Doctorly.Scheduling.Domain.Scheduling;

namespace Doctorly.Scheduling.Api.Contracts;

public sealed record RespondToInvitationRequest(AttendanceStatus Response);
