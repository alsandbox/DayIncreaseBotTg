namespace SunTgBot
{
    internal class WeatherApiManager
    {
        private readonly WeatherApiClient weatherApiClient;
        public double Latitude { get; set; }
        public double Longitude { get; set; }

        public WeatherApiManager(WeatherApiClient weatherApiClient)
        {
            this.weatherApiClient = weatherApiClient;
        }

        public async Task<string> GetTimeAsync(DateTime date)
        {
            DateTime yesterday = date.AddDays(-1).ToUniversalTime();
            var solstice = SolsticeData.GetSolsticeByYear(date.Year);

            if (solstice is null)
            {
                throw new ArgumentNullException(nameof(date));
            }

            try
            {
                string resultToday = await weatherApiClient.GetWeatherDataAsync(Latitude, Longitude, date);
                string resultYesterday = await weatherApiClient.GetWeatherDataAsync(Latitude, Longitude, yesterday);
                string resultShortestDay = await weatherApiClient.GetWeatherDataAsync(Latitude, Longitude, solstice.Value.Winter);

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
