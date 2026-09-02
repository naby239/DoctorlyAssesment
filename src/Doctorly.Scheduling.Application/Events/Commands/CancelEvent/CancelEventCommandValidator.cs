using Doctorly.Scheduling.Domain.Scheduling;
using FluentValidation;

namespace Doctorly.Scheduling.Application.Events.Commands.CancelEvent;

public sealed class CancelEventCommandValidator : AbstractValidator<CancelEventCommand>
{
    public CancelEventCommandValidator()
    {
        RuleFor(c => c.Id).NotEmpty();

        RuleFor(c => c.Reason)
            .MaximumLength(SchedulingLimits.CancellationReasonMaxLength);
    }
}
