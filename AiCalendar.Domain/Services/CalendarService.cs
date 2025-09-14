using System;
using System.Threading.Tasks;
using AiCalendar.Data.Entities;
using AiCalendar.Data.Repositories;

namespace AiCalendar.Domain.Services
{
    public class CalendarService : ICalendarService
    {
        private readonly IEventRepository _eventRepository;

        public CalendarService(IEventRepository eventRepository)
        {
            _eventRepository = eventRepository;
        }

        public async Task<Event> CreateEventAsync(Event newEvent)
        {
            return await _eventRepository.CreateEventAsync(newEvent);
        }

        public async Task<Event?> GetEventByIdAsync(int id)
        {
            return await _eventRepository.GetEventByIdAsync(id);
        }

        public async Task<Event> CreateEventFromTextAsync(string text)
        {
            // Placeholder logic for AI event creation
            var sampleEvent = new Event
            {
                Title = $"Sample Event from: {text}",
                StartTime = DateTime.Now,
                EndTime = DateTime.Now.AddHours(1),
                Location = "Sample Location"
            };
            return await _eventRepository.CreateEventAsync(sampleEvent);
        }
    }
}
