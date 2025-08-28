using Microsoft.Extensions.Configuration;

namespace DayIncrease;

internal class WeatherApiClient(IConfiguration configuration)
{
    private readonly string _apiUrl = configuration["ApiSettings:SunriseSunsetApiUrl"]
                                      ?? throw new ArgumentNullException(nameof(configuration),
                                          "API URL not configured");

    public async Task<string> GetWeatherDataAsync(double latitude, double longitude, DateTime date)
    {
        var formattedDate = date.ToString("yyyy-MM-dd");
        var apiUrl = $"{_apiUrl}?lat={latitude}&lng={longitude}&date={formattedDate}&formatted=0";

        using HttpClient client = new();
        try
        {
            var response = await client.GetAsync(apiUrl);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadAsStringAsync();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error fetching weather data: {ex.Message}");
            throw;
        }
    }
}