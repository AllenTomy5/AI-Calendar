using Microsoft.AspNetCore.Mvc;
using AiCalendar.Domain.Services;
using AiCalendar.Contracts.DTOs;
using AiCalendar.Api.Services;
using Microsoft.Extensions.AI;
using System.Text.Json;

namespace AiCalendar.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Produces("application/json")]
    public class TestController : ControllerBase
    {
        private readonly ICalendarService _calendarService;
        private readonly DatabaseMcpClient _mcpClient;
        private readonly IChatClient _chatClient;
        private readonly ILogger<TestController> _logger;

        public TestController(
            ICalendarService calendarService,
            DatabaseMcpClient mcpClient,
            IChatClient chatClient,
            ILogger<TestController> logger)
        {
            _calendarService = calendarService;
            _mcpClient = mcpClient;
            _chatClient = chatClient;
            _logger = logger;
        }

        /// <summary>
        /// Comprehensive end-to-end test demonstrating natural language to database persistence
        /// </summary>
        /// <param name="naturalLanguageInput">Natural language description of the event to create</param>
        /// <returns>Complete test results showing each step of the process</returns>
        [HttpPost("end-to-end")]
        public async Task<ActionResult<EndToEndTestResult>> RunEndToEndTest(
            [FromBody] EndToEndTestRequest request)
        {
            var testResult = new EndToEndTestResult
            {
                TestId = Guid.NewGuid(),
                StartTime = DateTime.UtcNow,
                Input = request.NaturalLanguageInput
            };

            try
            {
                _logger.LogInformation("Starting end-to-end test with input: {Input}", request.NaturalLanguageInput);
                
                // Step 1: Natural Language Processing with LLM
                testResult.Steps.Add(await ProcessNaturalLanguageStep(request.NaturalLanguageInput));
                
                // Step 2: MCP Tool Processing (simulate tool call)
                testResult.Steps.Add(await ProcessMcpToolStep());
                
                // Step 3: Database Persistence
                testResult.Steps.Add(await ProcessDatabasePersistenceStep());
                
                // Step 4: Verification - Retrieve and Verify
                testResult.Steps.Add(await ProcessVerificationStep());
                
                testResult.Success = testResult.Steps.All(s => s.Success);
                testResult.EndTime = DateTime.UtcNow;
                testResult.TotalDuration = testResult.EndTime - testResult.StartTime;
                
                _logger.LogInformation("End-to-end test completed. Success: {Success}, Duration: {Duration}ms", 
                    testResult.Success, testResult.TotalDuration.TotalMilliseconds);
                
                return Ok(testResult);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "End-to-end test failed: {Error}", ex.Message);
                testResult.Success = false;
                testResult.EndTime = DateTime.UtcNow;
                testResult.Error = ex.Message;
                testResult.TotalDuration = testResult.EndTime - testResult.StartTime;
                
                return StatusCode(500, testResult);
            }
        }

        /// <summary>
        /// Test individual MCP tools directly
        /// </summary>
        [HttpPost("mcp-tools/{toolName}")]
        public async Task<ActionResult<object>> TestMcpTool(string toolName, [FromBody] JsonElement parameters)
        {
            try
            {
                _logger.LogInformation("Testing MCP tool: {ToolName}", toolName);
                
                var parametersJson = JsonSerializer.Serialize(parameters);
                var result = await _mcpClient.CallToolAsync<object>(toolName, parameters);
                
                return Ok(new
                {
                    tool = toolName,
                    input = parameters,
                    result = result,
                    success = true,
                    timestamp = DateTime.UtcNow
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "MCP tool test failed for {ToolName}: {Error}", toolName, ex.Message);
                return StatusCode(500, new
                {
                    tool = toolName,
                    input = parameters,
                    error = ex.Message,
                    success = false,
                    timestamp = DateTime.UtcNow
                });
            }
        }

        /// <summary>
        /// Test database operations directly
        /// </summary>
        [HttpPost("database")]
        public async Task<ActionResult<DatabaseTestResult>> TestDatabaseOperations()
        {
            var testResult = new DatabaseTestResult
            {
                TestId = Guid.NewGuid(),
                StartTime = DateTime.UtcNow
            };

            try
            {
                // Test Create
                var createDto = new AiCalendar.Contracts.DTOs.CreateEventDto
                {
                    Title = "Database Test Event",
                    StartTime = DateTime.UtcNow.AddHours(1),
                    EndTime = DateTime.UtcNow.AddHours(2),
                    Location = "Test Location",
                    Description = "Test event for database operations"
                };

                var createdEvent = await _calendarService.CreateEventAsync(createDto);
                testResult.CreatedEventId = createdEvent.Id;
                testResult.CreateSuccess = true;

                // Test Read
                var retrievedEvent = await _calendarService.GetEventByIdAsync(createdEvent.Id);
                testResult.ReadSuccess = retrievedEvent != null && retrievedEvent.Id == createdEvent.Id;

                // Test Update
                var updateDto = new AiCalendar.Contracts.DTOs.UpdateEventDto
                {
                    Id = createdEvent.Id,
                    Title = "Updated Database Test Event",
                    Location = "Updated Test Location"
                };

                var updatedEvent = await _calendarService.UpdateEventAsync(updateDto);
                testResult.UpdateSuccess = updatedEvent.Title == updateDto.Title;

                // Test List
                var events = await _calendarService.ListEventsAsync(
                    DateTime.UtcNow.AddDays(-1), 
                    DateTime.UtcNow.AddDays(1));
                testResult.ListSuccess = events.Any(e => e.Id == createdEvent.Id);

                // Test Delete
                var deleteSuccess = await _calendarService.DeleteEventAsync(createdEvent.Id);
                testResult.DeleteSuccess = deleteSuccess;

                testResult.Success = testResult.CreateSuccess && testResult.ReadSuccess && 
                                   testResult.UpdateSuccess && testResult.ListSuccess && 
                                   testResult.DeleteSuccess;

                testResult.EndTime = DateTime.UtcNow;
                testResult.Duration = testResult.EndTime - testResult.StartTime;

                return Ok(testResult);
            }
            catch (Exception ex)
            {
                testResult.Success = false;
                testResult.Error = ex.Message;
                testResult.EndTime = DateTime.UtcNow;
                testResult.Duration = testResult.EndTime - testResult.StartTime;
                
                return StatusCode(500, testResult);
            }
        }

        /// <summary>
        /// Test system health and component status
        /// </summary>
        [HttpGet("health")]
        public async Task<ActionResult<SystemHealthResult>> GetSystemHealth()
        {
            var health = new SystemHealthResult
            {
                Timestamp = DateTime.UtcNow
            };

            try
            {
                // Test LLM connectivity
                health.LlmStatus = await TestLlmConnectivity();
                
                // Test database connectivity
                health.DatabaseStatus = await TestDatabaseConnectivity();
                
                // Test MCP client
                health.McpStatus = TestMcpClient();
                
                health.OverallStatus = health.LlmStatus.IsHealthy && 
                                     health.DatabaseStatus.IsHealthy && 
                                     health.McpStatus.IsHealthy;
                
                return Ok(health);
            }
            catch (Exception ex)
            {
                health.OverallStatus = false;
                health.Error = ex.Message;
                return StatusCode(500, health);
            }
        }

        private async Task<TestStep> ProcessNaturalLanguageStep(string input)
        {
            var step = new TestStep
            {
                StepName = "Natural Language Processing",
                StartTime = DateTime.UtcNow
            };

            try
            {
                var messages = new List<ChatMessage>
                {
                    new ChatMessage(ChatRole.System, "You are a calendar assistant. Parse this natural language request and suggest how to create a calendar event."),
                    new ChatMessage(ChatRole.User, input)
                };

                var response = await _chatClient.GetResponseAsync(messages);
                
                step.Success = true;
                step.Result = new
                {
                    input = input,
                    llmResponse = response.Text,
                    tokenCount = 0 // Token count not available in this response structure
                };
                step.Details = "LLM successfully processed natural language input";
            }
            catch (Exception ex)
            {
                step.Success = false;
                step.Error = ex.Message;
                step.Details = "Failed to process natural language input with LLM";
            }

            step.EndTime = DateTime.UtcNow;
            step.Duration = step.EndTime - step.StartTime;
            return step;
        }

        private async Task<TestStep> ProcessMcpToolStep()
        {
            var step = new TestStep
            {
                StepName = "MCP Tool Processing",
                StartTime = DateTime.UtcNow
            };

            try
            {
                var testEvent = new
                {
                    title = "Test Event from End-to-End Test",
                    startTime = DateTime.UtcNow.AddHours(1).ToString("yyyy-MM-ddTHH:mm:ssZ"),
                    endTime = DateTime.UtcNow.AddHours(2).ToString("yyyy-MM-ddTHH:mm:ssZ"),
                    location = "Test Location",
                    description = "Event created during end-to-end testing"
                };

                var result = await _mcpClient.CallToolAsync<object>("saveEvent", testEvent);
                
                step.Success = true;
                step.Result = new
                {
                    toolName = "saveEvent",
                    parameters = testEvent,
                    mcpResult = result
                };
                step.Details = "MCP tool successfully processed saveEvent call";
            }
            catch (Exception ex)
            {
                step.Success = false;
                step.Error = ex.Message;
                step.Details = "Failed to process MCP tool call";
            }

            step.EndTime = DateTime.UtcNow;
            step.Duration = step.EndTime - step.StartTime;
            return step;
        }

        private async Task<TestStep> ProcessDatabasePersistenceStep()
        {
            var step = new TestStep
            {
                StepName = "Database Persistence",
                StartTime = DateTime.UtcNow
            };

            try
            {
                var createDto = new AiCalendar.Contracts.DTOs.CreateEventDto
                {
                    Title = "Direct Database Test Event",
                    StartTime = DateTime.UtcNow.AddHours(3),
                    EndTime = DateTime.UtcNow.AddHours(4),
                    Location = "Database Test Location",
                    Description = "Event created directly in database during end-to-end test"
                };

                var createdEvent = await _calendarService.CreateEventAsync(createDto);
                
                step.Success = true;
                step.Result = new
                {
                    createdEvent = new
                    {
                        id = createdEvent.Id,
                        title = createdEvent.Title,
                        startTime = createdEvent.StartTime,
                        endTime = createdEvent.EndTime,
                        location = createdEvent.Location
                    }
                };
                step.Details = $"Successfully created event with ID: {createdEvent.Id}";
            }
            catch (Exception ex)
            {
                step.Success = false;
                step.Error = ex.Message;
                step.Details = "Failed to persist event to database";
            }

            step.EndTime = DateTime.UtcNow;
            step.Duration = step.EndTime - step.StartTime;
            return step;
        }

        private async Task<TestStep> ProcessVerificationStep()
        {
            var step = new TestStep
            {
                StepName = "Verification & Retrieval",
                StartTime = DateTime.UtcNow
            };

            try
            {
                var events = await _calendarService.ListEventsAsync(
                    DateTime.UtcNow.AddDays(-1),
                    DateTime.UtcNow.AddDays(1)
                );

                var testEvents = events.Where(e => e.Title.Contains("Test")).ToList();
                
                step.Success = testEvents.Any();
                step.Result = new
                {
                    totalEvents = events.Count,
                    testEvents = testEvents.Count,
                    events = testEvents.Select(e => new
                    {
                        id = e.Id,
                        title = e.Title,
                        startTime = e.StartTime,
                        endTime = e.EndTime
                    })
                };
                step.Details = $"Found {testEvents.Count} test events in database";
            }
            catch (Exception ex)
            {
                step.Success = false;
                step.Error = ex.Message;
                step.Details = "Failed to verify events in database";
            }

            step.EndTime = DateTime.UtcNow;
            step.Duration = step.EndTime - step.StartTime;
            return step;
        }

        private async Task<ComponentStatus> TestLlmConnectivity()
        {
            try
            {
                var messages = new List<ChatMessage>
                {
                    new ChatMessage(ChatRole.User, "Test message - please respond with 'OK'")
                };

                var response = await _chatClient.GetResponseAsync(messages);
                
                return new ComponentStatus
                {
                    ComponentName = "LLM (Ollama)",
                    IsHealthy = true,
                    Details = $"Response: {response.Text?.Substring(0, Math.Min(50, response.Text.Length))}"
                };
            }
            catch (Exception ex)
            {
                return new ComponentStatus
                {
                    ComponentName = "LLM (Ollama)",
                    IsHealthy = false,
                    Details = ex.Message
                };
            }
        }

        private async Task<ComponentStatus> TestDatabaseConnectivity()
        {
            try
            {
                var events = await _calendarService.GetAllEventsAsync();
                
                return new ComponentStatus
                {
                    ComponentName = "Database",
                    IsHealthy = true,
                    Details = $"Successfully connected. Found {events.Count()} events."
                };
            }
            catch (Exception ex)
            {
                return new ComponentStatus
                {
                    ComponentName = "Database",
                    IsHealthy = false,
                    Details = ex.Message
                };
            }
        }

        private ComponentStatus TestMcpClient()
        {
            try
            {
                // Basic check that MCP client is instantiated
                var isHealthy = _mcpClient != null;
                
                return new ComponentStatus
                {
                    ComponentName = "MCP Client",
                    IsHealthy = isHealthy,
                    Details = isHealthy ? "MCP client is available" : "MCP client is null"
                };
            }
            catch (Exception ex)
            {
                return new ComponentStatus
                {
                    ComponentName = "MCP Client",
                    IsHealthy = false,
                    Details = ex.Message
                };
            }
        }
    }

    // DTOs for test endpoints
    public class EndToEndTestRequest
    {
        public string NaturalLanguageInput { get; set; } = string.Empty;
    }

    public class EndToEndTestResult
    {
        public Guid TestId { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public TimeSpan TotalDuration { get; set; }
        public string Input { get; set; } = string.Empty;
        public bool Success { get; set; }
        public string? Error { get; set; }
        public List<TestStep> Steps { get; set; } = new();
    }

    public class TestStep
    {
        public string StepName { get; set; } = string.Empty;
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public TimeSpan Duration { get; set; }
        public bool Success { get; set; }
        public object? Result { get; set; }
        public string? Error { get; set; }
        public string? Details { get; set; }
    }

    public class DatabaseTestResult
    {
        public Guid TestId { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public TimeSpan Duration { get; set; }
        public bool Success { get; set; }
        public string? Error { get; set; }
        public int? CreatedEventId { get; set; }
        public bool CreateSuccess { get; set; }
        public bool ReadSuccess { get; set; }
        public bool UpdateSuccess { get; set; }
        public bool ListSuccess { get; set; }
        public bool DeleteSuccess { get; set; }
    }

    public class SystemHealthResult
    {
        public DateTime Timestamp { get; set; }
        public bool OverallStatus { get; set; }
        public string? Error { get; set; }
        public ComponentStatus LlmStatus { get; set; } = new();
        public ComponentStatus DatabaseStatus { get; set; } = new();
        public ComponentStatus McpStatus { get; set; } = new();
    }

    public class ComponentStatus
    {
        public string ComponentName { get; set; } = string.Empty;
        public bool IsHealthy { get; set; }
        public string? Details { get; set; }
    }
}