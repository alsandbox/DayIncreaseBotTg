using System.Net.Http;

using Microsoft.Extensions.Configuration;

namespace DayIncrease
{
    internal class WeatherApiClient
    {
        private readonly string ApiUrl;

        public WeatherApiClient(IConfiguration configuration)
        {
            ApiUrl = configuration["ApiSettings:SunriseSunsetApiUrl"]
                     ?? throw new ArgumentNullException(nameof(configuration), "API URL not configured");
        }

        public async Task<string> GetWeatherDataAsync(double latitude, double longitude, DateTime date)
        {
            string formattedDate = date.ToString("yyyy-MM-dd");
            string apiUrl = $"{ApiUrl}?lat={latitude}&lng={longitude}&date={formattedDate}&formatted=0";

            using HttpClient client = new();
            try
            {
                HttpResponseMessage response = await client.GetAsync(apiUrl);
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
}
