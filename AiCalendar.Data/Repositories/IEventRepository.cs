using System.Threading.Tasks;
using AiCalendar.Data.Entities;

namespace AiCalendar.Data.Repositories
{
    public interface IEventRepository
    {
        Task<Event> CreateEventAsync(Event newEvent);
        Task<Event?> GetEventByIdAsync(int id);
    }
}
