namespace AiCalendar.Api.DTOs
{
    public record CreateEventDto(
        string Title,
        string Description,
        DateTime StartTime,
        DateTime EndTime,
        List<string> AttendeeEmails
    );
}