using System;
using System.Threading.Tasks;
using AiCalendar.Data.Entities;
using AiCalendar.Data.Repositories;
using AiCalendar.Contracts.DTOs;
using AiCalendar.Domain.Exceptions;
using Microsoft.Extensions.Logging;

namespace AiCalendar.Domain.Services
{
    public class CalendarService : ICalendarService
    {
        private readonly IEventRepository _eventRepository;
        private readonly ILogger<CalendarService> _logger;

        public CalendarService(IEventRepository eventRepository, ILogger<CalendarService> logger)
        {
            _eventRepository = eventRepository;
            _logger = logger;
        }

        public async Task<Event> CreateEventAsync(CreateEventDto createEventDto)
        {
            try
            {
                _logger.LogInformation("Creating new event: {Title}", createEventDto.Title);
                
                // Validate input
                ValidateCreateEventDto(createEventDto);

                var newEvent = new Event
                {
                    Title = createEventDto.Title,
                    StartTime = createEventDto.StartTime,
                    EndTime = createEventDto.EndTime,
                    Location = createEventDto.Location ?? string.Empty,
                    Description = createEventDto.Description ?? string.Empty,
                    Timezone = "UTC",
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };
                
                var result = await _eventRepository.CreateEventAsync(newEvent);
                _logger.LogInformation("Successfully created event with ID: {EventId}", result.Id);
                return result;
            }
            catch (ValidationException)
            {
                throw; // Re-throw validation exceptions as-is
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create event: {Title}", createEventDto.Title);
                throw new DatabaseOperationException("CreateEvent", ex);
            }
        }

        public async Task<bool> DeleteEventAsync(int id)
        {
            try
            {
                _logger.LogInformation("Deleting event with ID: {EventId}", id);
                
                if (id <= 0)
                    throw new ValidationException("id", "Event ID must be greater than 0");

                var result = await _eventRepository.DeleteEventAsync(id);
                if (result)
                    _logger.LogInformation("Successfully deleted event with ID: {EventId}", id);
                else
                    _logger.LogWarning("Event with ID {EventId} not found for deletion", id);
                
                return result;
            }
            catch (ValidationException)
            {
                throw; // Re-throw as-is
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to delete event with ID: {EventId}", id);
                throw new DatabaseOperationException("DeleteEvent", ex);
            }
        }

        public async Task<Event> UpdateEventAsync(UpdateEventDto updateEventDto)
        {
            try
            {
                _logger.LogInformation("Updating event with ID: {EventId}", updateEventDto.Id);
                
                // Validate input
                ValidateUpdateEventDto(updateEventDto);

                var existing = await _eventRepository.GetEventByIdAsync(updateEventDto.Id);
                if (existing == null)
                {
                    _logger.LogWarning("Attempted to update non-existent event with ID: {EventId}", updateEventDto.Id);
                    throw new EventNotFoundException(updateEventDto.Id);
                }

                // Update properties if provided
                if (!string.IsNullOrEmpty(updateEventDto.Title))
                    existing.Title = updateEventDto.Title;
                if (updateEventDto.StartTime.HasValue)
                    existing.StartTime = updateEventDto.StartTime.Value;
                if (updateEventDto.EndTime.HasValue)
                    existing.EndTime = updateEventDto.EndTime.Value;
                if (!string.IsNullOrEmpty(updateEventDto.Location))
                    existing.Location = updateEventDto.Location;
                if (!string.IsNullOrEmpty(updateEventDto.Description))
                    existing.Description = updateEventDto.Description;

                existing.UpdatedAt = DateTime.UtcNow;
                
                var result = await _eventRepository.UpdateEventAsync(existing);
                _logger.LogInformation("Successfully updated event with ID: {EventId}", updateEventDto.Id);
                return result;
            }
            catch (EventNotFoundException)
            {
                throw; // Re-throw as-is
            }
            catch (ValidationException)
            {
                throw; // Re-throw as-is
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to update event with ID: {EventId}", updateEventDto.Id);
                throw new DatabaseOperationException("UpdateEvent", ex);
            }
        }

        public async Task<List<Event>> ListEventsAsync(DateTime startDate, DateTime endDate)
        {
            try
            {
                _logger.LogInformation("Listing events from {StartDate} to {EndDate}", startDate, endDate);
                
                ValidateDateRange(startDate, endDate);
                
                var result = await _eventRepository.GetEventsInRangeAsync(startDate, endDate);
                _logger.LogInformation("Found {Count} events in date range", result.Count);
                return result;
            }
            catch (ValidationException)
            {
                throw; // Re-throw as-is
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to list events from {StartDate} to {EndDate}", startDate, endDate);
                throw new DatabaseOperationException("ListEvents", ex);
            }
        }

        public async Task<Event?> GetEventByIdAsync(int id)
        {
            try
            {
                _logger.LogInformation("Getting event by ID: {EventId}", id);
                
                if (id <= 0)
                    throw new ValidationException("id", "Event ID must be greater than 0");

                var result = await _eventRepository.GetEventByIdAsync(id);
                if (result == null)
                    _logger.LogInformation("Event with ID {EventId} not found", id);
                else
                    _logger.LogInformation("Successfully retrieved event with ID: {EventId}", id);
                
                return result;
            }
            catch (ValidationException)
            {
                throw; // Re-throw as-is
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get event by ID: {EventId}", id);
                throw new DatabaseOperationException("GetEventById", ex);
            }
        }

        public async Task<IEnumerable<Event>> GetAllEventsAsync()
        {
            try
            {
                _logger.LogInformation("Getting all events");
                var result = await _eventRepository.GetAllEventsAsync();
                _logger.LogInformation("Successfully retrieved {Count} events", result.Count());
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get all events");
                throw new DatabaseOperationException("GetAllEvents", ex);
            }
        }

        public async Task<Event> CreateEventFromTextAsync(string text)
        {
            try
            {
                _logger.LogInformation("Creating event from text: {Text}", text);
                
                if (string.IsNullOrWhiteSpace(text))
                    throw new ValidationException("text", "Input text cannot be empty");

                // Placeholder logic for AI event creation
                var sampleEvent = new Event
                {
                    Title = $"Sample Event from: {text}",
                    StartTime = DateTime.UtcNow.AddHours(1),
                    EndTime = DateTime.UtcNow.AddHours(2),
                    Location = "Sample Location",
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };
                
                var result = await _eventRepository.CreateEventAsync(sampleEvent);
                _logger.LogInformation("Successfully created event from text with ID: {EventId}", result.Id);
                return result;
            }
            catch (ValidationException)
            {
                throw; // Re-throw as-is
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create event from text: {Text}", text);
                throw new DatabaseOperationException("CreateEventFromText", ex);
            }
        }

        public async Task InviteAttendeesAsync(int eventId, params IEnumerable<string> attendeeEmails)
        {
            foreach (var email in attendeeEmails.SelectMany(e => e))
            {
                // Invite logic here
            }
            await Task.CompletedTask;
        }

        private void ValidateCreateEventDto(CreateEventDto dto)
        {
            var errors = new Dictionary<string, string[]>();

            if (string.IsNullOrWhiteSpace(dto.Title))
                errors["title"] = new[] { "Title is required and cannot be empty" };
            else if (dto.Title.Length > 200)
                errors["title"] = new[] { "Title cannot exceed 200 characters" };

            if (dto.StartTime >= dto.EndTime)
                errors["time"] = new[] { "Start time must be before end time" };

            if (dto.StartTime < DateTime.UtcNow.AddMinutes(-5)) // Allow 5-minute buffer for clock skew
                errors["startTime"] = new[] { "Start time cannot be in the past" };

            if (!string.IsNullOrEmpty(dto.Location) && dto.Location.Length > 300)
                errors["location"] = new[] { "Location cannot exceed 300 characters" };

            if (!string.IsNullOrEmpty(dto.Description) && dto.Description.Length > 1000)
                errors["description"] = new[] { "Description cannot exceed 1000 characters" };

            if (errors.Any())
                throw new ValidationException("Event validation failed", errors);
        }

        private void ValidateUpdateEventDto(UpdateEventDto dto)
        {
            var errors = new Dictionary<string, string[]>();

            if (dto.Id <= 0)
                errors["id"] = new[] { "Event ID must be greater than 0" };

            if (!string.IsNullOrEmpty(dto.Title))
            {
                if (string.IsNullOrWhiteSpace(dto.Title))
                    errors["title"] = new[] { "Title cannot be empty if provided" };
                else if (dto.Title.Length > 200)
                    errors["title"] = new[] { "Title cannot exceed 200 characters" };
            }

            if (dto.StartTime.HasValue && dto.EndTime.HasValue && dto.StartTime >= dto.EndTime)
                errors["time"] = new[] { "Start time must be before end time" };

            if (!string.IsNullOrEmpty(dto.Location) && dto.Location.Length > 300)
                errors["location"] = new[] { "Location cannot exceed 300 characters" };

            if (!string.IsNullOrEmpty(dto.Description) && dto.Description.Length > 1000)
                errors["description"] = new[] { "Description cannot exceed 1000 characters" };

            if (errors.Any())
                throw new ValidationException("Event update validation failed", errors);
        }

        private void ValidateDateRange(DateTime startDate, DateTime endDate)
        {
            if (startDate >= endDate)
                throw new ValidationException("dateRange", "Start date must be before end date");

            if ((endDate - startDate).TotalDays > 365)
                throw new ValidationException("dateRange", "Date range cannot exceed 1 year");
        }
    }
}
