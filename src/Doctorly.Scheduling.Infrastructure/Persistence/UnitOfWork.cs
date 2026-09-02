using Doctorly.Scheduling.Application.Common.Interfaces;

namespace Doctorly.Scheduling.Infrastructure.Persistence;

internal sealed class UnitOfWork(SchedulingDbContext context) : IUnitOfWork
{
    public Task<int> SaveChangesAsync(CancellationToken cancellationToken) =>
        context.SaveChangesAsync(cancellationToken);
}
