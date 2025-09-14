using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.AI;
using OllamaSharp;

namespace AiCalendar.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class DiagnosticsController : ControllerBase
    {
        private readonly IChatClient _chatClient;
        private readonly ILogger<DiagnosticsController> _logger;

        public DiagnosticsController(IChatClient chatClient, ILogger<DiagnosticsController> logger)
        {
            _chatClient = chatClient;
            _logger = logger;
        }

        [HttpGet("client-type")]
        public IActionResult GetClientType()
        {
            var clientType = _chatClient.GetType().Name;
            var clientAssembly = _chatClient.GetType().Assembly.GetName().Name;
            
            var result = new
            {
                ClientType = clientType,
                Assembly = clientAssembly,
                IsOllama = _chatClient is OllamaApiClient,
                IsMock = clientType.Contains("Mock"),
                FullTypeName = _chatClient.GetType().FullName
            };

            _logger.LogInformation("Chat client diagnosis: Type={ClientType}, Assembly={Assembly}, IsOllama={IsOllama}", 
                clientType, clientAssembly, result.IsOllama);

            return Ok(result);
        }

        [HttpPost("test-llm")]
        public async Task<IActionResult> TestLlm([FromBody] TestLlmRequest request)
        {
            try
            {
                var clientType = _chatClient.GetType().Name;
                _logger.LogInformation("Testing LLM with client type: {ClientType}", clientType);

                var startTime = DateTime.UtcNow;
                
                var messages = new[]
                {
                    new ChatMessage(ChatRole.User, request.Prompt ?? "Hello, please respond with exactly: 'REAL_LLM_RESPONSE'")
                };

                var response = await _chatClient.GetResponseAsync(messages);
                var endTime = DateTime.UtcNow;
                var duration = endTime - startTime;

                var result = new
                {
                    ClientType = clientType,
                    IsOllama = _chatClient is OllamaApiClient,
                    Request = request.Prompt,
                    Response = response.Text,
                    Duration = duration.TotalMilliseconds,
                    Success = true,
                    ContainsMockResponse = response.Text?.Contains("mock") == true || response.Text?.Contains("Mock") == true
                };

                _logger.LogInformation("LLM test completed. Client={ClientType}, Duration={Duration}ms, Response length={Length}", 
                    clientType, duration.TotalMilliseconds, response.Text?.Length ?? 0);

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "LLM test failed with client type: {ClientType}", _chatClient.GetType().Name);
                
                return Ok(new
                {
                    ClientType = _chatClient.GetType().Name,
                    IsOllama = _chatClient is OllamaApiClient,
                    Success = false,
                    Error = ex.Message,
                    ErrorType = ex.GetType().Name
                });
            }
        }
    }

    public class TestLlmRequest
    {
        public string? Prompt { get; set; }
    }
}