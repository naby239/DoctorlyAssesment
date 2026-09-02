using Doctorly.Scheduling.Domain.Scheduling;
using FluentValidation;

namespace Doctorly.Scheduling.Application.Events.Commands.ScheduleEvent;

public sealed class ScheduleEventCommandValidator : AbstractValidator<ScheduleEventCommand>
{
    public ScheduleEventCommandValidator()
    {
        RuleFor(c => c.Title)
            .NotEmpty()
            .MaximumLength(SchedulingLimits.EventTitleMaxLength);

        RuleFor(c => c.Description)
            .MaximumLength(SchedulingLimits.EventDescriptionMaxLength);

        RuleFor(c => c.EndTime)
            .GreaterThan(c => c.StartTime)
            .WithMessage("'End Time' must be after 'Start Time'.");

        RuleForEach(c => c.Attendees).ChildRules(attendee =>
        {
            attendee.RuleFor(a => a.Name)
                .NotEmpty()
                .MaximumLength(SchedulingLimits.AttendeeNameMaxLength);

            attendee.RuleFor(a => a.Email)
                .NotEmpty()
                .MaximumLength(SchedulingLimits.EmailAddressMaxLength);

            attendee.RuleFor(a => a.ContactNumber)
                .MaximumLength(SchedulingLimits.ContactNumberMaxLength);
        });

        RuleFor(c => c.Attendees)
            .Must(a => a is null || a.Where(x => x.Email is not null)
                .Select(x => x.Email!.Trim().ToLowerInvariant())
                .Distinct(StringComparer.Ordinal).Count() == a.Count(x => x.Email is not null))
            .WithMessage("The same attendee cannot be invited twice.");
    }
}
