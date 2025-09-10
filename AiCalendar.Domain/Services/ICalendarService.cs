using System.Threading.Tasks;
using AiCalendar.Data.Entities;

namespace AiCalendar.Domain.Services
{
    public interface ICalendarService
    {
        Task<Event> CreateEventAsync(Event newEvent);
        Task<Event?> GetEventByIdAsync(int id);
        Task<Event> CreateEventFromTextAsync(string text);
    }
}
