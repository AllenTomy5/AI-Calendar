using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using AiCalendar.Contracts.DTOs;

namespace AiCalendar.Domain.Services
{
    public interface IMcpClient
    {
        Task<McpResponse<T>> CallToolAsync<T>(string toolName, object parameters);
    }

    public class McpClient : IMcpClient
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;
        private readonly ILogger<McpClient> _logger;

        public McpClient(HttpClient httpClient, IConfiguration configuration, ILogger<McpClient> logger)
        {
            _httpClient = httpClient;
            _configuration = configuration;
            _logger = logger;
        }

        public async Task<McpResponse<T>> CallToolAsync<T>(string toolName, object parameters)
        {
            try
            {
                var mcpServerUrl = _configuration["MCP:ServerUrl"] ?? "http://localhost:3000";
                var requestPayload = new
                {
                    tool = toolName,
                    parameters = parameters
                };

                var json = JsonSerializer.Serialize(requestPayload);
                var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");

                _logger.LogInformation("Calling MCP tool: {ToolName} with parameters: {Parameters}", toolName, json);

                var response = await _httpClient.PostAsync($"{mcpServerUrl}/tools/call", content);
                var responseText = await response.Content.ReadAsStringAsync();

                _logger.LogInformation("MCP response: {Response}", responseText);

                if (!response.IsSuccessStatusCode)
                {
                    return new McpResponse<T>
                    {
                        Ok = false,
                        Error = $"HTTP {response.StatusCode}: {responseText}",
                        Data = default
                    };
                }

                var mcpResponse = JsonSerializer.Deserialize<McpResponse<T>>(responseText, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                return mcpResponse ?? new McpResponse<T>
                {
                    Ok = false,
                    Error = "Failed to deserialize MCP response",
                    Data = default
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calling MCP tool: {ToolName}", toolName);
                return new McpResponse<T>
                {
                    Ok = false,
                    Error = ex.Message,
                    Data = default
                };
            }
        }
    }

    public class McpResponse<T>
    {
        public bool Ok { get; set; }
        public T? Data { get; set; }
        public string? Error { get; set; }
    }
}