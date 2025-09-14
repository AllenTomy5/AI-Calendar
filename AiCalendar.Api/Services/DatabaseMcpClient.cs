using System.Text.Json;
using Microsoft.Extensions.Logging;
using AiCalendar.Domain.Services;
using AiCalendar.Data.Entities;
using AiCalendar.Data.Repositories;

namespace AiCalendar.Api.Services
{
    public class DatabaseMcpClient : IMcpClient
    {
        private readonly IEventRepository _eventRepository;
        private readonly ILogger<DatabaseMcpClient> _logger;

        public DatabaseMcpClient(IEventRepository eventRepository, ILogger<DatabaseMcpClient> logger)
        {
            _eventRepository = eventRepository;
            _logger = logger;
        }

        public async Task<McpResponse<T>> CallToolAsync<T>(string toolName, object parameters)
        {
            try
            {
                _logger.LogInformation("MCP Tool Call: {ToolName} with parameters: {Parameters}", 
                    toolName, JsonSerializer.Serialize(parameters));

                return toolName switch
                {
                    "calendar.save_event" => await HandleSaveEvent<T>(parameters),
                    "calendar.update_event" => await HandleUpdateEvent<T>(parameters),
                    "calendar.cancel_event" => await HandleCancelEvent<T>(parameters),
                    "calendar.list_events" => await HandleListEvents<T>(parameters),
                    _ => CreateErrorResponse<T>($"Unknown tool: {toolName}")
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error executing MCP tool {ToolName}", toolName);
                return CreateErrorResponse<T>($"Internal error: {ex.Message}");
            }
        }

        private async Task<McpResponse<T>> HandleSaveEvent<T>(object parameters)
        {
            try
            {
                var paramJson = JsonSerializer.Serialize(parameters);
                var saveParams = JsonSerializer.Deserialize<SaveEventParams>(paramJson, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                if (saveParams == null)
                    return CreateErrorResponse<T>("Invalid parameters for save_event");

                // Validation
                var validationError = ValidateSaveEventParams(saveParams);
                if (validationError != null)
                    return CreateErrorResponse<T>(validationError);

                // Check for existing event with same client_reference_id (idempotency)
                Event? existingEvent = null;
                if (!string.IsNullOrEmpty(saveParams.ClientReferenceId))
                {
                    var allEvents = await _eventRepository.GetAllEventsAsync();
                    existingEvent = allEvents.FirstOrDefault(e => e.ClientReferenceId == saveParams.ClientReferenceId);
                }

                Event eventEntity;
                if (existingEvent != null)
                {
                    // Update existing event (idempotent behavior)
                    UpdateEventFromParams(existingEvent, saveParams);
                    eventEntity = await _eventRepository.UpdateEventAsync(existingEvent);
                    _logger.LogInformation("Updated existing event with client_reference_id: {ClientReferenceId}", 
                        saveParams.ClientReferenceId);
                }
                else
                {
                    // Create new event
                    eventEntity = CreateEventFromParams(saveParams);
                    eventEntity = await _eventRepository.CreateEventAsync(eventEntity);
                    _logger.LogInformation("Created new event with ID: {EventId}, Title: {Title}", 
                        eventEntity.Id, eventEntity.Title);
                }

                var response = new
                {
                    ok = true,
                    data = new
                    {
                        id = eventEntity.Id,
                        title = eventEntity.Title,
                        start = eventEntity.StartTime.ToString("yyyy-MM-ddTHH:mm:ssZ"),
                        end = eventEntity.EndTime.ToString("yyyy-MM-ddTHH:mm:ssZ"),
                        timezone = eventEntity.Timezone,
                        location = eventEntity.Location,
                        client_reference_id = eventEntity.ClientReferenceId
                    }
                };

                return new McpResponse<T>
                {
                    Ok = true,
                    Data = (T)(object)response,
                    Error = null
                };
            }
            catch (Exception ex)
            {
                return CreateErrorResponse<T>($"Save event error: {ex.Message}");
            }
        }

        private async Task<McpResponse<T>> HandleUpdateEvent<T>(object parameters)
        {
            try
            {
                var paramJson = JsonSerializer.Serialize(parameters);
                var updateParams = JsonSerializer.Deserialize<UpdateEventParams>(paramJson, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                if (updateParams == null)
                    return CreateErrorResponse<T>("Invalid parameters for update_event");

                // Find event by ID or client_reference_id
                Event? eventEntity = null;
                if (updateParams.Id.HasValue)
                {
                    eventEntity = await _eventRepository.GetEventByIdAsync(updateParams.Id.Value);
                }
                else if (!string.IsNullOrEmpty(updateParams.ClientReferenceId))
                {
                    var allEvents = await _eventRepository.GetAllEventsAsync();
                    eventEntity = allEvents.FirstOrDefault(e => e.ClientReferenceId == updateParams.ClientReferenceId);
                }

                if (eventEntity == null)
                    return CreateErrorResponse<T>("Event not found");

                // Apply partial updates
                if (!string.IsNullOrEmpty(updateParams.Title))
                    eventEntity.Title = updateParams.Title.Trim();
                
                if (updateParams.Start.HasValue)
                    eventEntity.StartTime = updateParams.Start.Value;
                
                if (updateParams.End.HasValue)
                    eventEntity.EndTime = updateParams.End.Value;
                
                if (updateParams.Location != null)
                    eventEntity.Location = updateParams.Location.Trim();
                
                if (updateParams.Notes != null)
                    eventEntity.Notes = updateParams.Notes.Trim();

                eventEntity.UpdatedAt = DateTime.UtcNow;

                // Validate after updates
                if (eventEntity.EndTime <= eventEntity.StartTime)
                    return CreateErrorResponse<T>("End time must be after start time");

                await _eventRepository.UpdateEventAsync(eventEntity);

                _logger.LogInformation("Updated event ID: {EventId}", eventEntity.Id);

                var response = new { ok = true };
                return new McpResponse<T>
                {
                    Ok = true,
                    Data = (T)(object)response,
                    Error = null
                };
            }
            catch (Exception ex)
            {
                return CreateErrorResponse<T>($"Update event error: {ex.Message}");
            }
        }

        private async Task<McpResponse<T>> HandleCancelEvent<T>(object parameters)
        {
            try
            {
                var paramJson = JsonSerializer.Serialize(parameters);
                var cancelParams = JsonSerializer.Deserialize<CancelEventParams>(paramJson, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                if (cancelParams == null)
                    return CreateErrorResponse<T>("Invalid parameters for cancel_event");

                // Find event by ID or client_reference_id
                Event? eventEntity = null;
                if (cancelParams.Id.HasValue)
                {
                    eventEntity = await _eventRepository.GetEventByIdAsync(cancelParams.Id.Value);
                }
                else if (!string.IsNullOrEmpty(cancelParams.ClientReferenceId))
                {
                    var allEvents = await _eventRepository.GetAllEventsAsync();
                    eventEntity = allEvents.FirstOrDefault(e => e.ClientReferenceId == cancelParams.ClientReferenceId);
                }

                if (eventEntity == null)
                    return CreateErrorResponse<T>("Event not found");

                await _eventRepository.DeleteEventAsync(eventEntity.Id);

                _logger.LogInformation("Cancelled event ID: {EventId}", eventEntity.Id);

                var response = new { ok = true };
                return new McpResponse<T>
                {
                    Ok = true,
                    Data = (T)(object)response,
                    Error = null
                };
            }
            catch (Exception ex)
            {
                return CreateErrorResponse<T>($"Cancel event error: {ex.Message}");
            }
        }

        private async Task<McpResponse<T>> HandleListEvents<T>(object parameters)
        {
            try
            {
                var events = await _eventRepository.GetAllEventsAsync();
                var eventList = events.Select(e => new
                {
                    id = e.Id,
                    title = e.Title,
                    start = e.StartTime.ToString("yyyy-MM-ddTHH:mm:ssZ"),
                    end = e.EndTime.ToString("yyyy-MM-ddTHH:mm:ssZ"),
                    timezone = e.Timezone,
                    location = e.Location,
                    client_reference_id = e.ClientReferenceId
                }).ToList();

                var response = new
                {
                    ok = true,
                    data = eventList
                };

                return new McpResponse<T>
                {
                    Ok = true,
                    Data = (T)(object)response,
                    Error = null
                };
            }
            catch (Exception ex)
            {
                return CreateErrorResponse<T>($"List events error: {ex.Message}");
            }
        }

        private string? ValidateSaveEventParams(SaveEventParams saveParams)
        {
            if (string.IsNullOrWhiteSpace(saveParams.Title))
                return "Title is required";

            if (saveParams.Start == default)
                return "Start time is required";

            if (saveParams.End == default)
                return "End time is required";

            if (saveParams.End <= saveParams.Start)
                return "End time must be after start time";

            return null;
        }

        private Event CreateEventFromParams(SaveEventParams saveParams)
        {
            return new Event
            {
                Title = saveParams.Title.Trim(),
                StartTime = saveParams.Start,
                EndTime = saveParams.End,
                Location = saveParams.Location?.Trim(),
                Description = saveParams.Description?.Trim(),
                Attendees = saveParams.Attendees != null ? JsonSerializer.Serialize(saveParams.Attendees) : null,
                Notes = saveParams.Notes?.Trim(),
                ClientReferenceId = saveParams.ClientReferenceId,
                Timezone = saveParams.Timezone ?? "UTC",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
        }

        private void UpdateEventFromParams(Event eventEntity, SaveEventParams saveParams)
        {
            eventEntity.Title = saveParams.Title.Trim();
            eventEntity.StartTime = saveParams.Start;
            eventEntity.EndTime = saveParams.End;
            eventEntity.Location = saveParams.Location?.Trim();
            eventEntity.Description = saveParams.Description?.Trim();
            eventEntity.Attendees = saveParams.Attendees != null ? JsonSerializer.Serialize(saveParams.Attendees) : null;
            eventEntity.Notes = saveParams.Notes?.Trim();
            eventEntity.Timezone = saveParams.Timezone ?? "UTC";
            eventEntity.UpdatedAt = DateTime.UtcNow;
        }

        private McpResponse<T> CreateErrorResponse<T>(string error)
        {
            return new McpResponse<T>
            {
                Ok = false,
                Data = default,
                Error = error
            };
        }
    }

    // Parameter classes for MCP tools
    public class SaveEventParams
    {
        public string Title { get; set; } = string.Empty;
        public DateTime Start { get; set; }
        public DateTime End { get; set; }
        public string? Timezone { get; set; }
        public string? Location { get; set; }
        public string[]? Attendees { get; set; }
        public string? Notes { get; set; }
        public string? Description { get; set; }
        public string? ClientReferenceId { get; set; }
    }

    public class UpdateEventParams
    {
        public int? Id { get; set; }
        public string? ClientReferenceId { get; set; }
        public string? Title { get; set; }
        public DateTime? Start { get; set; }
        public DateTime? End { get; set; }
        public string? Location { get; set; }
        public string? Notes { get; set; }
    }

    public class CancelEventParams
    {
        public int? Id { get; set; }
        public string? ClientReferenceId { get; set; }
    }
}