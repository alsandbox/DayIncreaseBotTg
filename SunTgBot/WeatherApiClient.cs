using System.Net.Http;

using Microsoft.Extensions.Configuration;

namespace SunTgBot
{
    internal class WeatherApiClient
    {
        private readonly string ApiUrl;

        public WeatherApiClient(IConfiguration configuration)
        {
            ApiUrl = configuration["ApiSettings:SunriseSunsetApiUrl"]
                     ?? throw new ArgumentNullException(nameof(configuration), "API URL not configured");
        }

        public async Task<string> GetWeatherDataAsync(float latitude, float longitude, DateTime date, string tzId)
        {
            string formattedDate = date.ToString("yyyy-MM-dd");
            string apiUrl = $"{ApiUrl}?lat={latitude}&lng={longitude}&date={formattedDate}&formatted=0&tzId={tzId}";

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
