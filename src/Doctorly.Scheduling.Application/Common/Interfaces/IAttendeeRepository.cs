using Doctorly.Scheduling.Domain.Scheduling;

namespace Doctorly.Scheduling.Application.Common.Interfaces;

public interface IAttendeeRepository
{
    Task<Attendee?> FindByEmailAsync(string email, CancellationToken cancellationToken);

    Task AddAsync(Attendee attendee, CancellationToken cancellationToken);
}
