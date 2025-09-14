// Add a default landing page for root URL
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Serilog;
using Microsoft.EntityFrameworkCore;
using FluentValidation.AspNetCore;
using Microsoft.Extensions.AI;
using OllamaSharp;
using FluentValidation;
using AiCalendar.Api.Middleware;
Log.Logger = new LoggerConfiguration()
    .Enrich.FromLogContext()
    .WriteTo.Console(new Serilog.Formatting.Json.JsonFormatter())
    .CreateLogger();

var builder = WebApplication.CreateBuilder(args);

// Set Serilog as the logger for the host
builder.Host.UseSerilog();

// Configure JWT Bearer authentication
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new Microsoft.IdentityModel.Tokens.TokenValidationParameters
        {
            ValidateIssuer = false,
            ValidateAudience = false,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = false
            // Configure as needed for your environment
        };
    });


builder.Services.AddAuthorization();
builder.Services.AddControllers();
// Add modern FluentValidation configuration
builder.Services.AddFluentValidationAutoValidation();
builder.Services.AddFluentValidationClientsideAdapters();
builder.Services.AddValidatorsFromAssemblyContaining<Program>();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Configure Ollama Chat Client for AI processing with proper error handling
builder.Services.AddSingleton<IChatClient>(serviceProvider =>
{
    var configuration = serviceProvider.GetRequiredService<IConfiguration>();
    var logger = serviceProvider.GetRequiredService<ILogger<Program>>();
    var ollamaBaseUrl = configuration["Ollama:BaseUrl"] ?? "http://localhost:11434";
    var ollamaModelName = configuration["Ollama:ModelName"] ?? "mistral";
    
    // Try to create OllamaApiClient - if successful, it means the configuration is correct
    // The actual connection will be tested when making requests
    try
    {
        var ollamaClient = new OllamaApiClient(new Uri(ollamaBaseUrl), ollamaModelName);
        logger.LogInformation("Successfully configured OllamaSharp ChatClient with model {ModelName} at {BaseUrl}. " +
            "Note: Actual connectivity will be tested when making requests.", ollamaModelName, ollamaBaseUrl);
        return ollamaClient;
    }
    catch (Exception ex)
    {
        logger.LogWarning("Failed to configure Ollama client at {BaseUrl}. Using MockChatClient as fallback. Error: {Error}", 
            ollamaBaseUrl, ex.Message);
        return new AiCalendar.Api.Services.MockChatClient();
    }
});
// });

// Register AI services
//builder.Services.AddHttpClient<AiCalendar.Domain.Services.IMcpClient, AiCalendar.Domain.Services.McpClient>();
builder.Services.AddScoped<AiCalendar.Domain.Services.IMcpClient, AiCalendar.Api.Services.DatabaseMcpClient>();
builder.Services.AddScoped<AiCalendar.Domain.Services.ILlmService, AiCalendar.Domain.Services.LlmService>();
builder.Services.AddScoped<AiCalendar.Domain.Services.ICalendarService, AiCalendar.Domain.Services.CalendarService>();
builder.Services.AddScoped<AiCalendar.Data.Repositories.IEventRepository, AiCalendar.Data.Repositories.EventRepository>();
builder.Services.AddDbContext<AiCalendar.Data.CalendarDbContext>(options =>
{
    options.UseInMemoryDatabase("AiCalendarDb"); // Or configure your real DB here
});


var app = builder.Build();

// Register custom exception handling middleware FIRST - TEMPORARILY DISABLED
app.UseGlobalExceptionHandler();

// Enable Swagger middleware
app.UseSwagger();
app.UseSwaggerUI();

app.UseSerilogRequestLogging();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.MapGet("/", context =>
{
    context.Response.Redirect("/swagger");
    return Task.CompletedTask;
});
app.MapGet("/favicon.ico", () => Results.File(Array.Empty<byte>(), "image/x-icon"));

app.Run();
