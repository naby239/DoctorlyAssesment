using Doctorly.Scheduling.Domain.Common;
using Doctorly.Scheduling.Domain.Scheduling;

namespace Doctorly.Scheduling.Domain.Tests;

public sealed class AttendeeTests
{
    [Fact]
    public void Registering_captures_the_supplied_details()
    {
        var attendee = Attendee.Register("Anna Weber", "anna.weber@practice.de", "+49 30 1234567");

        attendee.Name.ShouldBe("Anna Weber");
        attendee.Email.ShouldBe("anna.weber@practice.de");
        attendee.ContactNumber.ShouldBe("+49 30 1234567");
        attendee.Id.ShouldNotBe(Guid.Empty);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void An_attendee_must_have_a_name(string? name)
    {
        Should.Throw<DomainException>(() => Attendee.Register(name, "anna@practice.de"));
    }

    [Fact]
    public void A_name_longer_than_the_limit_is_rejected()
    {
        var tooLong = new string('a', SchedulingLimits.AttendeeNameMaxLength + 1);

        Should.Throw<DomainException>(() => Attendee.Register(tooLong, "anna@practice.de"));
    }

    [Fact]
    public void Surrounding_whitespace_is_trimmed_from_the_name()
    {
        var attendee = Attendee.Register("  Anna Weber  ", "anna@practice.de");

        attendee.Name.ShouldBe("Anna Weber");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void An_attendee_must_have_an_email_address(string? email)
    {
        Should.Throw<DomainException>(() => Attendee.Register("Anna Weber", email));
    }

    [Fact]
    public void Email_addresses_are_stored_lower_case()
    {
        var attendee = Attendee.Register("Anna Weber", "  Anna.Weber@Practice.DE  ");

        attendee.Email.ShouldBe("anna.weber@practice.de");
    }

    [Theory]
    [InlineData("not-an-email")]
    [InlineData("@practice.de")]
    [InlineData("anna@")]
    [InlineData("anna@@practice.de")]
    [InlineData("anna@practice@de")]
    public void Malformed_email_addresses_are_rejected(string email)
    {
        Should.Throw<DomainException>(() => Attendee.Register("Anna Weber", email));
    }

    [Fact]
    public void An_email_longer_than_the_limit_is_rejected()
    {
        var tooLong = new string('a', SchedulingLimits.EmailAddressMaxLength) + "@practice.de";

        Should.Throw<DomainException>(() => Attendee.Register("Anna Weber", tooLong));
    }

    [Fact]
    public void A_contact_number_is_optional()
    {
        var attendee = Attendee.Register("Anna Weber", "anna@practice.de");

        attendee.ContactNumber.ShouldBeNull();
    }

    [Fact]
    public void A_blank_contact_number_is_stored_as_null()
    {
        var attendee = Attendee.Register("Anna Weber", "anna@practice.de", "   ");

        attendee.ContactNumber.ShouldBeNull();
    }

    [Fact]
    public void A_contact_number_longer_than_the_limit_is_rejected()
    {
        var tooLong = new string('1', SchedulingLimits.ContactNumberMaxLength + 1);

        Should.Throw<DomainException>(() => Attendee.Register("Anna Weber", "anna@practice.de", tooLong));
    }
}
