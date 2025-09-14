// Add a default landing page for root URL
using Microsoft.AspNetCore.Authentication.JwtBearer;

using Serilog;
using Microsoft.EntityFrameworkCore;


// Configure Serilog for structured JSON logging
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
builder.Services.AddScoped<AiCalendar.Domain.Services.ICalendarService, AiCalendar.Domain.Services.CalendarService>();
builder.Services.AddScoped<AiCalendar.Data.Repositories.IEventRepository, AiCalendar.Data.Repositories.EventRepository>();
builder.Services.AddDbContext<AiCalendar.Data.CalendarDbContext>(options =>
{
    options.UseInMemoryDatabase("AiCalendarDb"); // Or configure your real DB here
});

var app = builder.Build();



app.UseSerilogRequestLogging();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.MapGet("/", () => "AI Calendar API is running. Use /api/calendar for REST endpoints.");
app.MapGet("/favicon.ico", () => Results.File(Array.Empty<byte>(), "image/x-icon"));

app.Run();
