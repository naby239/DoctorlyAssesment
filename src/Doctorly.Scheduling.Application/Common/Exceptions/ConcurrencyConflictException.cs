namespace Doctorly.Scheduling.Application.Common.Exceptions;

// Raised when the event changed between the caller reading it and writing it back.
public sealed class ConcurrencyConflictException : Exception
{
    public ConcurrencyConflictException(string message)
        : base(message)
    {
    }

    public ConcurrencyConflictException(string message, Exception innerException)
        : base(message, innerException)
    {
    }

    public ConcurrencyConflictException()
        : base("The event was modified by someone else.")
    {
    }
}
