using Microsoft.AspNetCore.Mvc;

namespace BasicsExample.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class WeatherController : ControllerBase
{
    private static readonly string[] Summaries =
    [
        "Freezing",
        "Bracing",
        "Chilly",
        "Cool",
        "Mild",
        "Warm",
        "Balmy",
        "Hot",
        "Sweltering",
        "Scorching"
    ];

    private readonly ILogger<WeatherController> _logger;

    public WeatherController(ILogger<WeatherController> logger)
    {
        _logger = logger;
    }

    [HttpGet]
    [ProducesResponseType<IReadOnlyCollection<WeatherForecast>>(StatusCodes.Status200OK)]
    public ActionResult<IReadOnlyCollection<WeatherForecast>> GetForecast(
        [FromQuery] int days = 5,
        [FromHeader(Name = "X-Correlation-Id")] string? correlationId = null)
    {
        if (days is < 1 or > 10)
        {
            return ValidationProblem(new Dictionary<string, string[]>
            {
                ["days"] = ["Days must be between 1 and 10."]
            });
        }

        _logger.LogInformation(
            "Generating {Days} weather forecasts. CorrelationId={CorrelationId}",
            days,
            correlationId ?? "none");

        var forecasts = Enumerable.Range(1, days)
            .Select(index =>
            {
                var temperatureC = Random.Shared.Next(-20, 55);
                return new WeatherForecast(
                    DateOnly.FromDateTime(DateTime.UtcNow.AddDays(index)),
                    temperatureC,
                    Summaries[Random.Shared.Next(Summaries.Length)]);
            })
            .ToArray();

        return Ok(forecasts);
    }

    [HttpGet("{date}")]
    [ProducesResponseType<WeatherForecast>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public ActionResult<WeatherForecast> GetForDate(DateOnly date)
    {
        if (date < DateOnly.FromDateTime(DateTime.UtcNow.Date))
        {
            return NotFound(new ProblemDetails
            {
                Title = "Forecast not available",
                Detail = "Historical weather is not stored by this sample controller.",
                Status = StatusCodes.Status404NotFound
            });
        }

        var temperatureC = Random.Shared.Next(-20, 55);
        return Ok(new WeatherForecast(
            date,
            temperatureC,
            Summaries[Random.Shared.Next(Summaries.Length)]));
    }
}

public sealed record WeatherForecast(
    DateOnly Date,
    int TemperatureC,
    string Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}

