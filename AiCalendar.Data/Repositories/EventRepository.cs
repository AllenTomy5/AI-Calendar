using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using AiCalendar.Data.Entities;

namespace AiCalendar.Data.Repositories
{
    public class EventRepository : IEventRepository
    {
        private readonly CalendarDbContext _context;

        public EventRepository(CalendarDbContext context)
        {
            _context = context;
        }

        public async Task<Event> CreateEventAsync(Event newEvent)
        {
            _context.Events.Add(newEvent);
            await _context.SaveChangesAsync();
            return newEvent;
        }

        public async Task<Event?> GetEventByIdAsync(int id)
        {
            return await _context.Events.FirstOrDefaultAsync(e => e.Id == id);
        }

        public async Task<IEnumerable<Event>> GetAllEventsAsync()
        {
            return await _context.Events.ToListAsync();
        }

        public async Task<Event> UpdateEventAsync(Event eventEntity)
        {
            _context.Events.Update(eventEntity);
            await _context.SaveChangesAsync();
            return eventEntity;
        }

        public async Task<bool> DeleteEventAsync(int id)
        {
            var eventEntity = await GetEventByIdAsync(id);
            if (eventEntity != null)
            {
                _context.Events.Remove(eventEntity);
                await _context.SaveChangesAsync();
                return true;
            }
            return false;
        }

        public async Task<List<Event>> GetEventsInRangeAsync(DateTime startDate, DateTime endDate)
        {
            return await _context.Events
                .Where(e => e.StartTime >= startDate && e.StartTime <= endDate)
                .OrderBy(e => e.StartTime)
                .ToListAsync();
        }
    }
}
