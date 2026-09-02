using Doctorly.Scheduling.Application.Common.Interfaces;
using Doctorly.Scheduling.Domain.Scheduling;
using Microsoft.EntityFrameworkCore;

namespace Doctorly.Scheduling.Infrastructure.Persistence.Repositories;

internal sealed class AttendeeRepository(SchedulingDbContext context) : IAttendeeRepository
{
    public Task<Attendee?> FindByEmailAsync(string email, CancellationToken cancellationToken) =>
        context.Attendees.FirstOrDefaultAsync(a => a.Email == email, cancellationToken);

    public async Task AddAsync(Attendee attendee, CancellationToken cancellationToken) =>
        await context.Attendees.AddAsync(attendee, cancellationToken).ConfigureAwait(false);
}
