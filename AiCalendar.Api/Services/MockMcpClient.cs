using AiCalendar.Domain.Services;

namespace AiCalendar.Api.Services
{
    public class MockMcpClient : IMcpClient
    {
        public async Task<McpResponse<T>> CallToolAsync<T>(string toolName, object parameters)
        {
            await Task.Delay(100); // Simulate processing
            
            if (toolName == "calendar.save_event")
            {
                var successResult = new { success = true, event_id = Guid.NewGuid().ToString(), message = "Event created successfully" };
                return new McpResponse<T>
                {
                    Ok = true,
                    Data = (T)(object)successResult,
                    Error = null
                };
            }
            else
            {
                var errorResult = new { success = false, error = "Unknown tool" };
                return new McpResponse<T>
                {
                    Ok = false,
                    Data = (T)(object)errorResult,
                    Error = "Unknown tool"
                };
            }
        }
    }
}