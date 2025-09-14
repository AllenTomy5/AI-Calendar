using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using AiCalendar.Contracts.DTOs;

namespace AiCalendar.Domain.Services
{
    public interface ILlmService
    {
        Task<IntentClassificationResult> ClassifyIntentAsync(string userPrompt);
    }

    public class LlmService : ILlmService
    {
        private readonly IChatClient _chatClient;
        private readonly ILogger<LlmService> _logger;

        public LlmService(IChatClient chatClient, ILogger<LlmService> logger)
        {
            _chatClient = chatClient;
            _logger = logger;
        }

        public async Task<IntentClassificationResult> ClassifyIntentAsync(string userPrompt)
        {
            var systemPrompt = @"
You are an AI assistant that classifies user intents for a calendar system and extracts event information.

Your task is to:
1. Classify the user's intent as one of: create, update, cancel, list
2. Extract event information from the user's message
3. Determine which MCP tool should be called
4. Identify any missing required fields

Available MCP tools:
- calendar.save_event: For creating new events
- calendar.update_event: For updating existing events  
- calendar.cancel_event: For canceling events
- calendar.list_events: For listing events

Required fields for events:
- title (required)
- start (required) 
- end (required, must be after start)

Optional fields:
- timezone (default to user's timezone if not specified)
- location
- attendees (list of email addresses)
- notes
- client_reference_id (for idempotency)

Return your response as a JSON object with this exact structure:
{
  ""intent"": ""create|update|cancel|list"",
  ""confidence"": 0.95,
  ""extracted_event"": {
    ""title"": ""Meeting Title"",
    ""start"": ""2024-07-01T10:00:00Z"",
    ""end"": ""2024-07-01T11:00:00Z"",
    ""timezone"": ""UTC"",
    ""location"": ""Conference Room"",
    ""attendees"": [""email1@example.com""],
    ""notes"": ""Additional notes"",
    ""client_reference_id"": ""unique-id""
  },
  ""missing_fields"": [""field1"", ""field2""],
  ""tool_to_call"": ""calendar.save_event""
}

For ambiguous dates like 'tomorrow' or 'next week', make reasonable assumptions based on today's date.
For missing times, assume business hours (9 AM - 5 PM).
Always respond with valid JSON only.";

            try
            {
                var messages = new[]
                {
                    new ChatMessage(ChatRole.System, systemPrompt),
                    new ChatMessage(ChatRole.User, userPrompt)
                };

                // Call the actual LLM (either Ollama or Mock)
                var response = await _chatClient.GetResponseAsync(messages);
                var responseText = response.Text ?? string.Empty;

                _logger.LogInformation("LLM Response: {Response}", responseText);

                // Parse the JSON response
                var result = JsonSerializer.Deserialize<IntentClassificationResult>(responseText, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                return result ?? CreateFallbackResponse(userPrompt);
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, "Failed to parse LLM response as JSON");
                return CreateFallbackResponse(userPrompt);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during LLM classification (possibly Ollama not available)");
                return CreateFallbackResponse(userPrompt);
            }
        }

        private IntentClassificationResult CreateFallbackResponse(string userPrompt)
        {
            // Create a reasonable fallback response when LLM is not available
            var intent = DetermineIntentFromKeywords(userPrompt);
            
            return new IntentClassificationResult
            {
                Intent = intent,
                Confidence = 0.7,
                ExtractedEvent = new ExtractedEvent
                {
                    Title = ExtractTitleFromPrompt(userPrompt),
                    Start = DateTime.UtcNow.AddHours(1),
                    End = DateTime.UtcNow.AddHours(2),
                    Timezone = "UTC"
                },
                MissingFields = new List<string>(),
                ToolToCall = GetToolForIntent(intent)
            };
        }

        private string DetermineIntentFromKeywords(string prompt)
        {
            var lowerPrompt = prompt.ToLower();
            
            if (lowerPrompt.Contains("schedule") || lowerPrompt.Contains("create") || lowerPrompt.Contains("add") || lowerPrompt.Contains("book"))
                return "create";
            if (lowerPrompt.Contains("update") || lowerPrompt.Contains("change") || lowerPrompt.Contains("modify"))
                return "update";
            if (lowerPrompt.Contains("cancel") || lowerPrompt.Contains("delete") || lowerPrompt.Contains("remove"))
                return "cancel";
            if (lowerPrompt.Contains("list") || lowerPrompt.Contains("show") || lowerPrompt.Contains("view"))
                return "list";
                
            return "create"; // Default assumption
        }

        private string ExtractTitleFromPrompt(string prompt)
        {
            // Simple title extraction
            if (prompt.Length > 100)
                return prompt.Substring(0, 100) + "...";
            return prompt;
        }

        private string GetToolForIntent(string intent)
        {
            return intent switch
            {
                "create" => "calendar.save_event",
                "update" => "calendar.update_event", 
                "cancel" => "calendar.cancel_event",
                "list" => "calendar.list_events",
                _ => "calendar.save_event"
            };
        }
    }
}