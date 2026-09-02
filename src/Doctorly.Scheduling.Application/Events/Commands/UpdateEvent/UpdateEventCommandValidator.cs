using Doctorly.Scheduling.Domain.Scheduling;
using FluentValidation;

namespace Doctorly.Scheduling.Application.Events.Commands.UpdateEvent;

public sealed class UpdateEventCommandValidator : AbstractValidator<UpdateEventCommand>
{
    public UpdateEventCommandValidator()
    {
        RuleFor(c => c.Id).NotEmpty();

        RuleFor(c => c.Title)
            .NotEmpty()
            .MaximumLength(SchedulingLimits.EventTitleMaxLength);

        RuleFor(c => c.Description)
            .MaximumLength(SchedulingLimits.EventDescriptionMaxLength);

        RuleFor(c => c.EndTime)
            .GreaterThan(c => c.StartTime)
            .WithMessage("'End Time' must be after 'Start Time'.");
    }
}
