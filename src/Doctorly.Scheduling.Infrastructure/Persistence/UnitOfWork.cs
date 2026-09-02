using Doctorly.Scheduling.Application.Common.Exceptions;
using Doctorly.Scheduling.Application.Common.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Doctorly.Scheduling.Infrastructure.Persistence;

internal sealed class UnitOfWork(SchedulingDbContext context) : IUnitOfWork
{
    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken)
    {
        try
        {
            return await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (DbUpdateConcurrencyException ex)
        {
            // EF's exception is a persistence detail; the layers above only need to know the
            // write lost a race.
            throw new ConcurrencyConflictException(
                "This event has changed since you last read it. Fetch it again and reapply your changes.",
                ex);
        }
    }
}
