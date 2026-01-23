using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Extensions.Mcp;
using Microsoft.Extensions.Logging;

namespace TestAppIsolated;

public class WeatherFunction
{
    private readonly ILogger<WeatherFunction> _logger;
    private readonly WeatherService _weatherService;

    public WeatherFunction(ILogger<WeatherFunction> logger)
    {
        _logger = logger;
        _weatherService = new WeatherService(WeatherService.CreateDefaultClient());
    }

    [Function(nameof(GetWeatherWidget))]
    public string GetWeatherWidget(
        [McpResourceTrigger(
            "ui://widget/weather.html",
            "Weather Widget",
            MimeType = "text/html+skybridge",
            Description = "Interactive weather display for ChatGPT Apps")]
        [McpResourceMetadata("openai/widgetPrefersBorder", true)]
        [McpResourceMetadata("openai/widgetDomain", "https://chatgpt.com")]
        ResourceInvocationContext context)
    {
        var file = Path.Combine(AppContext.BaseDirectory, "resources", "weather.html");
        return File.ReadAllText(file);
    }

    // ["openai/outputTemplate"] = "ui://widget/weather.html"
    [Function(nameof(GetWeather))]
    public async Task<object> GetWeather(
        [McpToolTrigger(nameof(GetWeather), "Returns current weather for a location via Open-Meteo.")] ToolInvocationContext context,
        [McpToolProperty("location", "City name to check weather for (e.g., Seattle, New York, Miami).")] string location)
    {
        try
        {
            var result = await _weatherService.GetCurrentWeatherAsync(location);
            
            if (result is WeatherResult weather)
            {
                _logger.LogInformation("Weather fetched for {Location}: {TempC}°C", weather.Location, weather.TemperatureC);
            }
            else if (result is WeatherError error)
            {
                _logger.LogWarning("Weather error for {Location}: {Error}", error.Location, error.Error);
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get weather for {Location}", location);
            return new WeatherError(location ?? "Unknown", $"Unable to fetch weather: {ex.Message}", "api.open-meteo.com");
        }
    }
}
