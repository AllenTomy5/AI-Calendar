# AI Calendar System

A comprehensive AI-powered calendar management system that provides natural language processing for calendar operations through a Model Context Protocol (MCP) server integration with local LLM support.

## Architecture Overview

This system implements a 3-step AI Calendar architecture:

1. **LLM Integration**: Uses local Ollama LLM for natural language processing
2. **MCP Server**: Database-backed Model Context Protocol server for calendar operations
3. **Natural Language Orchestration**: Seamless coordination between AI and calendar functions

## Features

- **Natural Language Processing**: Create, update, and query calendar events using natural language
- **Local LLM**: Runs entirely offline using Ollama (no internet required)
- **Comprehensive Event Management**: Full CRUD operations with validation
- **Database Persistence**: Entity Framework Core with SQL Server support
- **MCP Protocol**: Standard Model Context Protocol implementation
- **Validation & Error Handling**: Comprehensive input validation and error reporting

## Prerequisites

### Required Software

1. **Ollama** - Local LLM runtime
   - Download from [https://ollama.ai](https://ollama.ai)
   - Install and ensure it's running on default port `11434`

2. **.NET 8 SDK** - Application runtime
   - Download from [https://dotnet.microsoft.com/download/dotnet/8.0](https://dotnet.microsoft.com/download/dotnet/8.0)

3. **SQL Server** (or SQL Server LocalDB)
   - For production: SQL Server 2019+
   - For development: SQL Server LocalDB (included with Visual Studio)

### Ollama Model Setup

```bash
# Pull the recommended model (llama3.2 or similar)
ollama pull llama3.2

# Verify Ollama is running
curl http://localhost:11434/api/version
```

## Installation & Setup

### 1. Clone and Build

```bash
git clone <repository-url>
cd Project
dotnet restore
dotnet build
```

### 2. Database Configuration

The application uses Entity Framework Core with in-memory database for development. For production, update `appsettings.json`:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=(localdb)\\mssqllocaldb;Database=AiCalendarDb;Trusted_Connection=true;MultipleActiveResultSets=true;"
  }
}
```

### 3. Environment Variables

Create `appsettings.Development.json`:

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "OllamaSettings": {
    "BaseUrl": "http://localhost:11434",
    "Model": "llama3.2"
  }
}
```

### 4. Run the Application

```bash
cd AiCalendar.Api
dotnet run
```

The API will be available at `https://localhost:7071` (or the port specified in launch settings).

## Usage Guide

### API Endpoints

#### Diagnostics & Health Checks

- `GET /diagnostics/llm-status` - Check LLM integration status
- `GET /diagnostics/test-llm` - Test LLM with sample query
- `GET /api/health` - General health check

#### Calendar Operations

- `POST /api/calendar/events` - Create new event
- `GET /api/calendar/events/{id}` - Get event by ID
- `GET /api/calendar/events` - List all events
- `PUT /api/calendar/events/{id}` - Update event
- `DELETE /api/calendar/events/{id}` - Delete event

### Natural Language Processing

The system processes natural language requests through the `/diagnostics/test-llm` endpoint:

```bash
# Example: Creating an event with natural language
curl -X GET "https://localhost:7071/diagnostics/test-llm?query=Schedule a team meeting for tomorrow at 2 PM in conference room A"
```

### MCP Tool Integration

The system exposes four main MCP tools:

1. **saveEvent** - Create new calendar events
2. **updateEvent** - Modify existing events
3. **cancelEvent** - Delete events
4. **listEvents** - Query events by date range

See [docs/mcp-tool-specifications.json](docs/mcp-tool-specifications.json) for complete tool specifications.

### Direct Database Operations

```bash
# Create event via API
curl -X POST "https://localhost:7071/api/calendar/events" \
  -H "Content-Type: application/json" \
  -d '{
    "title": "Team Meeting",
    "startTime": "2024-01-15T14:00:00Z",
    "endTime": "2024-01-15T15:00:00Z",
    "location": "Conference Room A",
    "description": "Weekly team standup"
  }'

# List events
curl -X GET "https://localhost:7071/api/calendar/events"

# Update event
curl -X PUT "https://localhost:7071/api/calendar/events/1" \
  -H "Content-Type: application/json" \
  -d '{
    "title": "Updated Meeting Title",
    "location": "Conference Room B"
  }'
```

## Testing & Verification

### End-to-End Testing Flow

1. **Start the application**
   ```bash
   dotnet run --project AiCalendar.Api
   ```

2. **Verify LLM connection**
   ```bash
   curl https://localhost:7071/diagnostics/llm-status
   ```
   Expected response: `{"isOllama": true, "status": "Connected", "model": "llama3.2"}`

3. **Test natural language processing**
   ```bash
   curl "https://localhost:7071/diagnostics/test-llm?query=Create a meeting for tomorrow at 3 PM"
   ```
   Should return natural language response from LLM.

4. **Test event creation**
   ```bash
   curl -X POST "https://localhost:7071/api/calendar/events" \
     -H "Content-Type: application/json" \
     -d '{
       "title": "Test Event",
       "startTime": "2024-01-15T15:00:00Z",
       "endTime": "2024-01-15T16:00:00Z"
     }'
   ```

5. **Verify event persistence**
   ```bash
   curl https://localhost:7071/api/calendar/events
   ```
   Should show the created event.

### Integration Testing

The system includes comprehensive integration tests covering:

- LLM connectivity and response processing
- Database operations (CRUD)
- MCP tool functionality
- End-to-end natural language to database persistence

Run tests:
```bash
dotnet test
```

## Troubleshooting

### Common Issues

#### 1. "Ollama not responding" or LLM connection failures

**Symptoms:**
- `/diagnostics/llm-status` returns connection errors
- `isOllama: false` in responses

**Solutions:**
- Verify Ollama is installed and running: `ollama --version`
- Check Ollama service: `curl http://localhost:11434/api/version`
- Ensure the model is available: `ollama list`
- Check firewall/port access to 11434

#### 2. Database connection issues

**Symptoms:**
- Entity Framework errors on startup
- "Cannot connect to database" errors

**Solutions:**
- Verify SQL Server/LocalDB is running
- Check connection string in `appsettings.json`
- Run database migrations: `dotnet ef database update`
- For development, the app uses in-memory database by default

#### 3. Slow LLM responses

**Symptoms:**
- Long response times (>30 seconds)
- Timeouts on LLM calls

**Solutions:**
- Ensure sufficient system RAM (8GB+ recommended)
- Use smaller models for faster responses
- Check system CPU/GPU utilization
- Consider adjusting timeout values in configuration

#### 4. Validation errors

**Symptoms:**
- "Required field missing" errors
- Date format validation failures

**Solutions:**
- Ensure all required fields are provided (title, startTime, endTime)
- Use ISO 8601 date format: `2024-01-15T14:00:00Z`
- Check field length limits (title: 200 chars, description: 1000 chars)

### Performance Considerations

- **LLM Response Time**: Local LLM processing typically takes 15-30 seconds
- **Database Operations**: In-memory database provides fast access for development
- **Concurrent Requests**: System supports multiple simultaneous calendar operations

### Development Setup

For development with hot reload:

```bash
# Terminal 1: Start Ollama
ollama serve

# Terminal 2: Run application with hot reload
dotnet watch run --project AiCalendar.Api
```

## Architecture Details

### Project Structure

- **AiCalendar.Api** - Web API controllers and startup configuration
- **AiCalendar.Domain** - Business logic and services
- **AiCalendar.Data** - Entity Framework, repositories, and database context
- **AiCalendar.Contracts** - DTOs and shared contracts
- **AiCalendar.MCP** - Model Context Protocol implementation

### Key Components

1. **CalendarService** - Core business logic for calendar operations
2. **DatabaseMcpClient** - MCP protocol implementation
3. **EventRepository** - Data access layer
4. **OllamaApiClient** - LLM integration (via Microsoft.Extensions.AI)

### Data Model

The `Event` entity includes:
- `Id` - Primary key
- `Title` - Event title (required)
- `StartTime` / `EndTime` - Event duration (required)
- `Location` / `Description` - Optional details
- `Timezone` - Timezone information
- `ClientReferenceId` - External reference tracking
- `CreatedAt` / `UpdatedAt` - Audit timestamps

## Contributing

1. Fork the repository
2. Create feature branch: `git checkout -b feature/new-feature`
3. Commit changes: `git commit -am 'Add new feature'`
4. Push to branch: `git push origin feature/new-feature`
5. Submit pull request

## License

This project is licensed under the MIT License - see the LICENSE file for details.

## Support

For issues and questions:
1. Check the troubleshooting section above
2. Review the [MCP tool specifications](docs/mcp-tool-specifications.json)
3. Test individual components using the diagnostic endpoints
4. Create an issue with detailed error messages and system information