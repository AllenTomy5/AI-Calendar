using Microsoft.AspNetCore.Mvc;
using AiCalendar.Domain.Services;
using AiCalendar.Data.Entities;
using System.Threading.Tasks;
using System.Collections.Generic; // Added for IEnumerable

namespace AiCalendar.Api.Controllers
{
    // A DTO (Data Transfer Object) to represent the data needed to create/update an event.
    // This is typically in its own file (e.g., in a "DTOs" folder).
    public record CreateEventDto(
        string Title,
        string Description,
        DateTime StartTime,
        DateTime EndTime,
        List<string> AttendeeEmails
    );

    [ApiController]
    [Route("api/[controller]")]
    public class CalendarController : ControllerBase
    {
        private readonly ICalendarService _calendarService;

        public CalendarController(ICalendarService calendarService)
        {
            _calendarService = calendarService;
        }

        // === EXISTING ENDPOINTS (Correct) ===

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

        // === NEWLY ADDED ENDPOINTS ===

        /// <summary>
        /// Gets all events.
        /// </summary>
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Event>>> GetAllEvents()
        {
            var events = await _calendarService.GetAllEventsAsync();
            return Ok(events);
        }

        /// <summary>
        /// Creates a new event from a JSON object.
        /// </summary>
        [HttpPost]
        public async Task<ActionResult<Event>> CreateEvent([FromBody] AiCalendar.Contracts.DTOs.CreateEventDto createEventDto)
        {
            var newEvent = await _calendarService.CreateEventAsync(createEventDto);
            return CreatedAtAction(nameof(GetEventById), new { id = newEvent.Id }, newEvent);
        }

        /// <summary>
        /// Updates/reschedules an existing event.
        /// </summary>
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateEvent(int id, [FromBody] AiCalendar.Contracts.DTOs.UpdateEventDto updateEventDto)
        {
            updateEventDto.Id = id; // Ensure the ID matches the route parameter
            var updatedEvent = await _calendarService.UpdateEventAsync(updateEventDto);
            return Ok(updatedEvent);
        }

        /// <summary>
        /// Deletes/cancels an event.
        /// </summary>
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteEvent(int id)
        {
            var success = await _calendarService.DeleteEventAsync(id);
            if (!success)
            {
                return NotFound();
            }
            return NoContent(); // Standard response for a successful delete
        }
    }
}