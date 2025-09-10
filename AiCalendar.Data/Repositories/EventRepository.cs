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
    }
}
