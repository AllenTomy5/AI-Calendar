using Microsoft.EntityFrameworkCore;
using AiCalendar.Data.Entities;

namespace AiCalendar.Data
{
    public class CalendarDbContext : DbContext
    {
        public CalendarDbContext(DbContextOptions<CalendarDbContext> options)
            : base(options)
        {
        }

        public DbSet<Event> Events { get; set; }
    }
}
