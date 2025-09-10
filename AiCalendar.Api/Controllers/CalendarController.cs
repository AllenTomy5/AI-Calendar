using Microsoft.AspNetCore.Mvc;
using AiCalendar.Domain.Services;
using AiCalendar.Data.Entities;
using System.Threading.Tasks;

namespace AiCalendar.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class CalendarController : ControllerBase
    {
        private readonly ICalendarService _calendarService;

        public CalendarController(ICalendarService calendarService)
        {
            _calendarService = calendarService;
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<Event>> GetEventById(int id)
        {
            var result = await _calendarService.GetEventByIdAsync(id);
            if (result == null)
                return NotFound();
            return Ok(result);
        }

        [HttpPost("parse")]
        public async Task<ActionResult<Event>> CreateEventFromText([FromBody] string text)
        {
            var createdEvent = await _calendarService.CreateEventFromTextAsync(text);
            return CreatedAtAction(nameof(GetEventById), new { id = createdEvent.Id }, createdEvent);
        }
    }
}
