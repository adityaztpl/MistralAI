using System.Diagnostics;
using AdvancedExample.Products;
using Microsoft.AspNetCore.Mvc;

namespace AdvancedExample.Middleware;

public sealed class ExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionMiddleware> _logger;
    private readonly IHostEnvironment _environment;

    public ExceptionMiddleware(
        RequestDelegate next,
        ILogger<ExceptionMiddleware> logger,
        IHostEnvironment environment)
    {
        _next = next;
        _logger = logger;
        _environment = environment;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (RequestValidationException ex)
        {
            await WriteValidationProblemAsync(context, ex);
        }
        catch (NotFoundException ex)
        {
            await WriteProblemAsync(
                context,
                StatusCodes.Status404NotFound,
                "Resource not found",
                ex.Message);
        }
        catch (ConflictException ex)
        {
            await WriteProblemAsync(
                context,
                StatusCodes.Status409Conflict,
                "Conflict",
                ex.Message);
        }
        catch (OperationCanceledException) when (context.RequestAborted.IsCancellationRequested)
        {
            _logger.LogInformation(
                "Request {Method} {Path} was canceled by the client",
                context.Request.Method,
                context.Request.Path);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Unhandled exception while processing {Method} {Path}",
                context.Request.Method,
                context.Request.Path);

            var detail = _environment.IsDevelopment()
                ? ex.ToString()
                : "An unexpected error occurred.";

            await WriteProblemAsync(
                context,
                StatusCodes.Status500InternalServerError,
                "Server error",
                detail);
        }
    }

    private static async Task WriteValidationProblemAsync(
        HttpContext context,
        RequestValidationException exception)
    {
        if (context.Response.HasStarted)
        {
            return;
        }

        context.Response.StatusCode = StatusCodes.Status400BadRequest;
        context.Response.ContentType = "application/problem+json";

        var problem = new ValidationProblemDetails(exception.Errors)
        {
            Type = "https://httpstatuses.com/400",
            Title = "Validation failed",
            Detail = exception.Message,
            Status = StatusCodes.Status400BadRequest,
            Instance = context.Request.Path
        };

        AddTraceId(context, problem);
        await context.Response.WriteAsJsonAsync(problem);
    }

    private static async Task WriteProblemAsync(
        HttpContext context,
        int statusCode,
        string title,
        string detail)
    {
        if (context.Response.HasStarted)
        {
            return;
        }

        context.Response.StatusCode = statusCode;
        context.Response.ContentType = "application/problem+json";

        var problem = new ProblemDetails
        {
            Type = $"https://httpstatuses.com/{statusCode}",
            Title = title,
            Detail = detail,
            Status = statusCode,
            Instance = context.Request.Path
        };

        AddTraceId(context, problem);
        await context.Response.WriteAsJsonAsync(problem);
    }

    private static void AddTraceId(HttpContext context, ProblemDetails problem)
    {
        problem.Extensions["traceId"] = Activity.Current?.Id ?? context.TraceIdentifier;
    }
}

public static class ExceptionMiddlewareExtensions
{
    public static IApplicationBuilder UseApiExceptionHandling(this IApplicationBuilder app)
    {
        return app.UseMiddleware<ExceptionMiddleware>();
    }
}

/*
Program.cs usage:

var app = builder.Build();

app.UseApiExceptionHandling();
app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
*/

