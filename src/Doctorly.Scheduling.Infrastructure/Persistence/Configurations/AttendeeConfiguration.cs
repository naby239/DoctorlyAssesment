using Doctorly.Scheduling.Domain.Scheduling;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Doctorly.Scheduling.Infrastructure.Persistence.Configurations;

internal sealed class AttendeeConfiguration : IEntityTypeConfiguration<Attendee>
{
    public void Configure(EntityTypeBuilder<Attendee> builder)
    {
        builder.ToTable("Attendees");

        builder.HasKey(a => a.Id);
        builder.Property(a => a.Id).ValueGeneratedNever();

        builder.Property(a => a.Name)
            .IsRequired()
            .HasMaxLength(SchedulingLimits.AttendeeNameMaxLength);

        builder.Property(a => a.Email)
            .IsRequired()
            .HasMaxLength(SchedulingLimits.EmailAddressMaxLength);

        builder.Property(a => a.ContactNumber)
            .HasMaxLength(SchedulingLimits.ContactNumberMaxLength);

        builder.Property(a => a.CreatedAt).IsRequired();

        // Reusing an attendee depends on the address identifying exactly one person.
        builder.HasIndex(a => a.Email).IsUnique();
    }
}
