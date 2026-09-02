using Doctorly.Scheduling.Domain.Scheduling;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Doctorly.Scheduling.Infrastructure.Persistence.Configurations;

internal sealed class CalendarEventConfiguration : IEntityTypeConfiguration<CalendarEvent>
{
    public void Configure(EntityTypeBuilder<CalendarEvent> builder)
    {
        builder.ToTable("Events");

        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).ValueGeneratedNever();

        builder.Property(e => e.Title)
            .IsRequired()
            .HasMaxLength(SchedulingLimits.EventTitleMaxLength);

        builder.Property(e => e.Description)
            .HasMaxLength(SchedulingLimits.EventDescriptionMaxLength);

        builder.Property(e => e.StartTime).IsRequired();
        builder.Property(e => e.EndTime).IsRequired();

        builder.Property(e => e.Status)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.Property(e => e.CancellationReason)
            .HasMaxLength(SchedulingLimits.CancellationReasonMaxLength);

        builder.Property(e => e.CreatedAt).IsRequired();

        builder.Property(e => e.Version).IsConcurrencyToken();

        builder.HasMany(e => e.Attendees)
            .WithOne()
            .HasForeignKey(ea => ea.EventId)
            .OnDelete(DeleteBehavior.Cascade);

        // Attendees is exposed read-only, so EF reads and writes the backing field.
        builder.Navigation(e => e.Attendees)
            .UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
