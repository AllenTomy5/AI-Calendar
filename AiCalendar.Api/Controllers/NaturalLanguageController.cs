using Microsoft.AspNetCore.Mvc;
using AiCalendar.Domain.Services;
using AiCalendar.Contracts.DTOs;
using System.Text.Json;

namespace AiCalendar.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class NaturalLanguageController : ControllerBase
    {
        private readonly ILlmService _llmService;
        private readonly IMcpClient _mcpClient;
        private readonly ILogger<NaturalLanguageController> _logger;

        public NaturalLanguageController(
            ILlmService llmService, 
            IMcpClient mcpClient,
            ILogger<NaturalLanguageController> logger)
        {
            _llmService = llmService;
            _mcpClient = mcpClient;
            _logger = logger;
        }

        [HttpPost("process")]
        public async Task<ActionResult<object>> ProcessNaturalLanguage([FromBody] NaturalLanguageRequest request)
        {
            try
            {
                _logger.LogInformation("Processing natural language request: {Prompt}", request.Prompt);

                // Step 1: Classify intent and extract entities using LLM
                var classification = await _llmService.ClassifyIntentAsync(request.Prompt);

                _logger.LogInformation("LLM classification result - Intent: {Intent}, Confidence: {Confidence}, Tool: {Tool}", 
                    classification.Intent, classification.Confidence, classification.ToolToCall);

                // Step 2: Check for missing required fields
                if (classification.MissingFields.Any())
                {
                    return BadRequest(new
                    {
                        success = false,
                        error = "Missing required information",
                        missing_fields = classification.MissingFields,
                        suggestion = "Please provide the missing information to complete your request"
                    });
                }

                // Step 3: Call appropriate MCP tool based on classification
                var mcpResponse = await CallMcpTool(classification);

                if (!mcpResponse.Ok)
                {
                    _logger.LogError("MCP call failed: {Error}", mcpResponse.Error);
                    return StatusCode(500, new
                    {
                        success = false,
                        error = mcpResponse.Error,
                        llm_output = classification,
                        mcp_call = classification.ToolToCall
                    });
                }

                // Step 4: Return success response with logs
                var response = new
                {
                    success = true,
                    result = mcpResponse.Data,
                    logs = new
                    {
                        llm_output = classification,
                        mcp_call = classification.ToolToCall,
                        db_operation = mcpResponse.Data
                    }
                };

                _logger.LogInformation("Successfully processed natural language request. DB ID: {DbId}", 
                    ExtractDbId(mcpResponse.Data));

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing natural language request");
                return StatusCode(500, new
                {
                    success = false,
                    error = "An internal error occurred while processing your request"
                });
            }
        }

        private async Task<McpResponse<object>> CallMcpTool(IntentClassificationResult classification)
        {
            switch (classification.ToolToCall)
            {
                case "calendar.save_event":
                    if (classification.ExtractedEvent == null)
                    {
                        return new McpResponse<object>
                        {
                            Ok = false,
                            Error = "No event data extracted for save operation"
                        };
                    }

                    return await _mcpClient.CallToolAsync<object>("calendar.save_event", new
                    {
                        title = classification.ExtractedEvent.Title,
                        start = classification.ExtractedEvent.Start?.ToString("yyyy-MM-ddTHH:mm:ssZ"),
                        end = classification.ExtractedEvent.End?.ToString("yyyy-MM-ddTHH:mm:ssZ"),
                        timezone = classification.ExtractedEvent.Timezone ?? "UTC",
                        location = classification.ExtractedEvent.Location,
                        attendees = classification.ExtractedEvent.Attendees,
                        notes = classification.ExtractedEvent.Notes,
                        client_reference_id = classification.ExtractedEvent.ClientReferenceId ?? Guid.NewGuid().ToString()
                    });

                case "calendar.update_event":
                    if (classification.ExtractedEvent == null)
                    {
                        return new McpResponse<object>
                        {
                            Ok = false,
                            Error = "No event data extracted for update operation"
                        };
                    }

                    var updateParams = new Dictionary<string, object>();
                    
                    if (!string.IsNullOrEmpty(classification.ExtractedEvent.ClientReferenceId))
                        updateParams["client_reference_id"] = classification.ExtractedEvent.ClientReferenceId;
                    
                    if (!string.IsNullOrEmpty(classification.ExtractedEvent.Title))
                        updateParams["title"] = classification.ExtractedEvent.Title;
                    
                    if (classification.ExtractedEvent.Start.HasValue)
                        updateParams["start"] = classification.ExtractedEvent.Start.Value.ToString("yyyy-MM-ddTHH:mm:ssZ");
                    
                    if (classification.ExtractedEvent.End.HasValue)
                        updateParams["end"] = classification.ExtractedEvent.End.Value.ToString("yyyy-MM-ddTHH:mm:ssZ");
                    
                    if (!string.IsNullOrEmpty(classification.ExtractedEvent.Location))
                        updateParams["location"] = classification.ExtractedEvent.Location;
                    
                    if (classification.ExtractedEvent.Attendees.Any())
                        updateParams["attendees"] = classification.ExtractedEvent.Attendees;
                    
                    if (!string.IsNullOrEmpty(classification.ExtractedEvent.Notes))
                        updateParams["notes"] = classification.ExtractedEvent.Notes;

                    return await _mcpClient.CallToolAsync<object>("calendar.update_event", updateParams);

                case "calendar.cancel_event":
                    if (classification.ExtractedEvent == null)
                    {
                        return new McpResponse<object>
                        {
                            Ok = false,
                            Error = "No event data extracted for cancel operation"
                        };
                    }

                    var cancelParams = new Dictionary<string, object>();
                    if (!string.IsNullOrEmpty(classification.ExtractedEvent.ClientReferenceId))
                        cancelParams["client_reference_id"] = classification.ExtractedEvent.ClientReferenceId;

                    return await _mcpClient.CallToolAsync<object>("calendar.cancel_event", cancelParams);

                case "calendar.list_events":
                    return await _mcpClient.CallToolAsync<object>("calendar.list_events", new
                    {
                        start_date = classification.ExtractedEvent?.Start?.ToString("yyyy-MM-ddTHH:mm:ssZ"),
                        end_date = classification.ExtractedEvent?.End?.ToString("yyyy-MM-ddTHH:mm:ssZ"),
                        limit = 50
                    });

                default:
                    return new McpResponse<object>
                    {
                        Ok = false,
                        Error = $"Unknown or unsupported tool: {classification.ToolToCall}"
                    };
            }
        }

        private string ExtractDbId(object? data)
        {
            if (data == null) return "unknown";

            try
            {
                var jsonString = JsonSerializer.Serialize(data);
                var jsonDoc = JsonDocument.Parse(jsonString);
                
                if (jsonDoc.RootElement.TryGetProperty("id", out var idElement))
                {
                    return idElement.ToString();
                }
                
                return "unknown";
            }
            catch
            {
                return "unknown";
            }
        }
    }

    public class NaturalLanguageRequest
    {
        public string Prompt { get; set; } = string.Empty;
    }
}