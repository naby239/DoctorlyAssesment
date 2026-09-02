using Doctorly.Scheduling.Domain.Scheduling;
using Microsoft.EntityFrameworkCore;

namespace Doctorly.Scheduling.Infrastructure.Persistence;

public sealed class SchedulingDbContext(DbContextOptions<SchedulingDbContext> options)
    : DbContext(options)
{
    public DbSet<CalendarEvent> Events => Set<CalendarEvent>();

    public DbSet<Attendee> Attendees => Set<Attendee>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(SchedulingDbContext).Assembly);

        base.OnModelCreating(modelBuilder);
    }
}
