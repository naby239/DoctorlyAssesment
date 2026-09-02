using System.Globalization;
using Doctorly.Scheduling.Domain.Common;

namespace Doctorly.Scheduling.Domain.Scheduling;

public sealed class Attendee
{
    private Attendee()
    {
    }

    public Guid Id { get; private set; }

    public string Name { get; private set; } = null!;

    public string Email { get; private set; } = null!;

    public string? ContactNumber { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public static Attendee Register(string? name, string? email, string? contactNumber = null) =>
        new()
        {
            Id = Guid.NewGuid(),
            Name = ValidateName(name),
            Email = ValidateEmail(email),
            ContactNumber = ValidateContactNumber(contactNumber),
            CreatedAt = DateTimeOffset.UtcNow,
        };

    private static string ValidateName(string? name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new DomainException("An attendee must have a name.");
        }

        var trimmed = name.Trim();

        if (trimmed.Length > SchedulingLimits.AttendeeNameMaxLength)
        {
            throw new DomainException(
                $"An attendee name cannot exceed {SchedulingLimits.AttendeeNameMaxLength} characters.");
        }

        return trimmed;
    }

    private static string ValidateEmail(string? email)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            throw new DomainException("An attendee must have an email address.");
        }

        // Lower-cased so the same person is matched rather than duplicated on their next visit.
        var normalised = email.Trim().ToLower(CultureInfo.InvariantCulture);

        if (normalised.Length > SchedulingLimits.EmailAddressMaxLength)
        {
            throw new DomainException(
                $"An email address cannot exceed {SchedulingLimits.EmailAddressMaxLength} characters.");
        }

        var at = normalised.IndexOf('@', StringComparison.Ordinal);

        if (at <= 0 || at != normalised.LastIndexOf('@') || at == normalised.Length - 1)
        {
            throw new DomainException($"'{email}' is not a valid email address.");
        }

        return normalised;
    }

    private static string? ValidateContactNumber(string? contactNumber)
    {
        if (string.IsNullOrWhiteSpace(contactNumber))
        {
            return null;
        }

        var trimmed = contactNumber.Trim();

        if (trimmed.Length > SchedulingLimits.ContactNumberMaxLength)
        {
            throw new DomainException(
                $"A contact number cannot exceed {SchedulingLimits.ContactNumberMaxLength} characters.");
        }

        return trimmed;
    }
}
