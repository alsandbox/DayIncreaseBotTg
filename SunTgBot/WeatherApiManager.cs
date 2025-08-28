namespace DayIncrease;

internal class WeatherApiManager(WeatherApiClient weatherApiClient)
{
    public double Latitude { get; set; }
    public double Longitude { get; set; }

    public async Task<string> GetTimeAsync(DateTime date)
    {
        var yesterday = date.AddDays(-1).ToUniversalTime();
        var solstice = SolsticeData.GetSolsticeByYear(date.Year);

        if (solstice is null) throw new ArgumentNullException(nameof(date));

        try
        {
            var resultToday = await weatherApiClient.GetWeatherDataAsync(Latitude, Longitude, date);
            var resultYesterday = await weatherApiClient.GetWeatherDataAsync(Latitude, Longitude, yesterday);
            var resultShortestDay =
                await weatherApiClient.GetWeatherDataAsync(Latitude, Longitude, solstice.Value.Winter);

            var sunriseTime = WeatherDataParser.ParseSunriseTime(resultToday);
            var sunsetTime = WeatherDataParser.ParseSunsetTime(resultToday);
            var dayLength = WeatherDataParser.ParseDayLength(resultToday, resultYesterday, resultShortestDay);

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