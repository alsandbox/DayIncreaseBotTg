namespace SunTgBot
{
    internal class WeatherApiManager
    {
        private readonly WeatherApiClient weatherApiClient;

        public WeatherApiManager(WeatherApiClient weatherApiClient)
        {
            this.weatherApiClient = weatherApiClient;
        }

        public async Task<string> GetTimeAsync(float latitude, float longitude, DateTime date, string tzId)
        {
            DateTime yesterday = date.AddDays(-1).ToUniversalTime();
            DateTime shortestDay = new DateTime(2023, 12, 22, 0, 0, 0, DateTimeKind.Utc);

            try
            {
                string resultToday = await weatherApiClient.GetWeatherDataAsync(latitude, longitude, date, tzId);
                string resultYesterday = await weatherApiClient.GetWeatherDataAsync(latitude, longitude, yesterday, tzId);
                string resultShortestDay = await weatherApiClient.GetWeatherDataAsync(latitude, longitude, shortestDay, tzId);

                string sunriseTime = WeatherDataParser.ParseSunriseTime(resultToday);
                string sunsetTime = WeatherDataParser.ParseSunsetTime(resultToday);
                string dayLength = WeatherDataParser.ParseDayLength(resultToday, resultYesterday, resultShortestDay);

                var weatherInfo = new
                {
                    SunriseTime = sunriseTime,
                    SunsetTime = sunsetTime,
                    DayLength = dayLength
                };

                return Newtonsoft.Json.JsonConvert.SerializeObject(weatherInfo);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
                return Newtonsoft.Json.JsonConvert.SerializeObject(new { Error = "Failed to fetch weather data" });
            }
        }
    }
}
