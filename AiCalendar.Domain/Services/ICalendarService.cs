using System.Threading.Tasks;
using AiCalendar.Data.Entities;
using AiCalendar.Contracts.DTOs;

namespace AiCalendar.Domain.Services
{
    public interface ICalendarService
    {
        Task<Event> CreateEventAsync(CreateEventDto createEventDto);
        Task<Event?> GetEventByIdAsync(int id);
        Task<IEnumerable<Event>> GetAllEventsAsync();
        Task<Event> CreateEventFromTextAsync(string text);
        Task InviteAttendeesAsync(int eventId, params IEnumerable<string> attendeeEmails);
        Task<bool> DeleteEventAsync(int id);
        Task<Event> UpdateEventAsync(UpdateEventDto updateEventDto);
        Task<List<Event>> ListEventsAsync(DateTime startDate, DateTime endDate);
    }
}
