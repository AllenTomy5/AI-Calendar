using System.Threading.Tasks;
using AiCalendar.Data.Entities;

namespace AiCalendar.Data.Repositories
{
    public interface IEventRepository
    {
        Task<Event> CreateEventAsync(Event newEvent);
        Task<Event?> GetEventByIdAsync(int id);
        Task<IEnumerable<Event>> GetAllEventsAsync();
        Task<Event> UpdateEventAsync(Event eventEntity);
        Task<bool> DeleteEventAsync(int id);
        Task<List<Event>> GetEventsInRangeAsync(DateTime startDate, DateTime endDate);
    }
}
