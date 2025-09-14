using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using AiCalendar.Domain.Exceptions;

namespace AiCalendar.Api.Middleware
{
    public class ExceptionHandlingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<ExceptionHandlingMiddleware> _logger;

        public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An unhandled exception occurred");
                await HandleExceptionAsync(context, ex);
            }
        }

        private static async Task HandleExceptionAsync(HttpContext context, Exception exception)
        {
            var response = context.Response;
            response.ContentType = "application/json";

            var problemDetails = exception switch
            {
                ValidationException validationEx => CreateProblemDetails(
                    "Validation Error",
                    validationEx.Message,
                    HttpStatusCode.BadRequest,
                    validationEx.Code,
                    validationEx.ValidationErrors),
                
                EventNotFoundException notFoundEx => CreateProblemDetails(
                    "Event Not Found",
                    notFoundEx.Message,
                    HttpStatusCode.NotFound,
                    notFoundEx.Code,
                    new { eventId = notFoundEx.EventId }),
                
                DatabaseOperationException dbEx => CreateProblemDetails(
                    "Database Operation Failed",
                    "A database error occurred while processing the request",
                    HttpStatusCode.InternalServerError,
                    dbEx.Code,
                    new { operation = dbEx.Message }),
                
                CalendarException calEx => CreateProblemDetails(
                    "Calendar Operation Error",
                    calEx.Message,
                    HttpStatusCode.BadRequest,
                    calEx.Code),
                
                _ => CreateProblemDetails(
                    "Internal Server Error",
                    "An unexpected error occurred while processing the request",
                    HttpStatusCode.InternalServerError,
                    "INTERNAL_ERROR")
            };

            response.StatusCode = problemDetails.Status ?? 500;

            var jsonResponse = JsonSerializer.Serialize(problemDetails, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                WriteIndented = true
            });

            await response.WriteAsync(jsonResponse);
        }

        private static ProblemDetails CreateProblemDetails(
            string title,
            string detail,
            HttpStatusCode statusCode,
            string? errorCode = null,
            object? extensions = null)
        {
            var problemDetails = new ProblemDetails
            {
                Title = title,
                Detail = detail,
                Status = (int)statusCode,
                Type = $"https://httpstatuses.com/{(int)statusCode}"
            };

            if (!string.IsNullOrEmpty(errorCode))
            {
                problemDetails.Extensions.Add("errorCode", errorCode);
            }

            if (extensions != null)
            {
                problemDetails.Extensions.Add("details", extensions);
            }

            return problemDetails;
        }
    }

    public static class ExceptionHandlingMiddlewareExtensions
    {
        public static IApplicationBuilder UseGlobalExceptionHandler(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<ExceptionHandlingMiddleware>();
        }
    }
}