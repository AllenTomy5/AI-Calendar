using System;

namespace AiCalendar.Domain.Exceptions
{
    public class CalendarException : Exception
    {
        public string Code { get; }

        public CalendarException(string code, string message) : base(message)
        {
            Code = code;
        }

        public CalendarException(string code, string message, Exception innerException) : base(message, innerException)
        {
            Code = code;
        }
    }

    public class EventNotFoundException : CalendarException
    {
        public int EventId { get; }

        public EventNotFoundException(int eventId) 
            : base("EVENT_NOT_FOUND", $"Event with ID {eventId} was not found")
        {
            EventId = eventId;
        }
    }

    public class ValidationException : CalendarException
    {
        public Dictionary<string, string[]> ValidationErrors { get; }

        public ValidationException(string message, Dictionary<string, string[]> errors) 
            : base("VALIDATION_ERROR", message)
        {
            ValidationErrors = errors ?? new Dictionary<string, string[]>();
        }

        public ValidationException(string field, string error) 
            : base("VALIDATION_ERROR", $"Validation failed for field '{field}': {error}")
        {
            ValidationErrors = new Dictionary<string, string[]>
            {
                [field] = new[] { error }
            };
        }
    }

    public class DatabaseOperationException : CalendarException
    {
        public DatabaseOperationException(string operation, Exception innerException) 
            : base("DATABASE_ERROR", $"Database operation '{operation}' failed: {innerException.Message}", innerException)
        {
        }
    }
}