using FluentValidation;

namespace Doctorly.Scheduling.Application.Events.Queries.ListEvents;

public sealed class ListEventsQueryValidator : AbstractValidator<ListEventsQuery>
{
    public const int MaxPageSize = 100;

    public ListEventsQueryValidator()
    {
        RuleFor(q => q.Page).GreaterThanOrEqualTo(1);

        RuleFor(q => q.PageSize).InclusiveBetween(1, MaxPageSize);

        RuleFor(q => q.To)
            .GreaterThan(q => q.From)
            .When(q => q.From.HasValue && q.To.HasValue)
            .WithMessage("'To' must be after 'From'.");

        RuleFor(q => q.Search).MaximumLength(200);
    }
}
