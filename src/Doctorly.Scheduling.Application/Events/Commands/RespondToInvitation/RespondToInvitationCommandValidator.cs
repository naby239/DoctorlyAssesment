using Doctorly.Scheduling.Domain.Scheduling;
using FluentValidation;

namespace Doctorly.Scheduling.Application.Events.Commands.RespondToInvitation;

public sealed class RespondToInvitationCommandValidator
    : AbstractValidator<RespondToInvitationCommand>
{
    public RespondToInvitationCommandValidator()
    {
        RuleFor(c => c.EventId).NotEmpty();
        RuleFor(c => c.AttendeeId).NotEmpty();

        RuleFor(c => c.Response)
            .Must(r => r is AttendanceStatus.Accepted or AttendanceStatus.Declined)
            .WithMessage("'Response' must be either Accepted or Declined.");
    }
}
