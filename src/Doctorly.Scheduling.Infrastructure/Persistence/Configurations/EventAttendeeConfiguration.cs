using Doctorly.Scheduling.Domain.Scheduling;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Doctorly.Scheduling.Infrastructure.Persistence.Configurations;

internal sealed class EventAttendeeConfiguration : IEntityTypeConfiguration<EventAttendee>
{
    public void Configure(EntityTypeBuilder<EventAttendee> builder)
    {
        builder.ToTable("EventAttendees");

        builder.HasKey(ea => ea.Id);
        builder.Property(ea => ea.Id).ValueGeneratedNever();

        builder.Property(ea => ea.Status)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.Property(ea => ea.OptInNotify).IsRequired();
        builder.Property(ea => ea.InvitedAt).IsRequired();

        builder.HasIndex(ea => new { ea.EventId, ea.AttendeeId }).IsUnique();

        // No navigation to Attendee - the aggregates are joined by id only. Restrict rather than
        // cascade so an attendee with invitations cannot be deleted out from under them.
        builder.HasOne<Attendee>()
            .WithMany()
            .HasForeignKey(ea => ea.AttendeeId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
