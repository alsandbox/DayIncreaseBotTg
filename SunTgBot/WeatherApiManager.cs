using static System.Runtime.InteropServices.JavaScript.JSType;

namespace SunTgBot
{
    internal class WeatherApiManager
    {
        private const string ApiUrl = "https://api.sunrise-sunset.org/json";

        public async Task<string> GetTimeAsync(float latitude, float longitude, DateTime date, string tzId)
        {
            DateTime yesterday = date.AddDays(-1);
            DateTime shortestDay = new DateTime(2023, 12, 22);

            string formattedDate = date.ToString("yyyy-MM-dd");
            string formattedYesterdayDate = yesterday.ToString("yyyy-MM-dd");
            string formattedShortestDay = shortestDay.ToString("yyyy-MM-dd");

            string apiUrlToday = $"{ApiUrl}?lat={latitude}&lng={longitude}&date={formattedDate}&formatted=0&tzId={tzId}";
            string apiUrlYesterday = $"{ApiUrl}?lat={latitude}&lng={longitude}&date={formattedYesterdayDate}&formatted=0&tzId={tzId}";
            string apiUrlShortestDay = $"{ApiUrl}?lat={latitude}&lng={longitude}&date={formattedShortestDay}&formatted=0&tzId={tzId}";

            WeatherData defaultWeatherData = new WeatherData();

            using (HttpClient client = new HttpClient())
            {
                try
                {
                    HttpResponseMessage responseToday = await client.GetAsync(apiUrlToday);
                    HttpResponseMessage responseYesterday = await client.GetAsync(apiUrlYesterday);
                    HttpResponseMessage responseShortestDay = await client.GetAsync(apiUrlShortestDay);

                    if (responseToday.IsSuccessStatusCode && responseYesterday.IsSuccessStatusCode)
                    {
                        string resultToday = await responseToday.Content.ReadAsStringAsync();
                        string resultYesterday = await responseYesterday.Content.ReadAsStringAsync();
                        string resultShortestDay = await responseShortestDay.Content.ReadAsStringAsync();

                        string sunriseTime = ParseSunriseTime(resultToday);
                        string sunsetTime = ParseSunsetTime(resultToday);
                        string dayLength = ParseDayLength(resultToday, resultYesterday, resultShortestDay);

                        var weatherInfo = new
                        {
                            SunriseTime = sunriseTime,
                            SunsetTime = sunsetTime,
                            DayLength = dayLength
                        };

                        string weatherInfoJson = Newtonsoft.Json.JsonConvert.SerializeObject(weatherInfo);

                        return weatherInfoJson;
                    }
                    else
                    {
                        Console.WriteLine($"Error: {responseToday.StatusCode}");
                        Console.WriteLine($"Error: {responseYesterday.StatusCode}");
                        return Newtonsoft.Json.JsonConvert.SerializeObject(defaultWeatherData);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Exception: {ex.Message}");
                    return Newtonsoft.Json.JsonConvert.SerializeObject(defaultWeatherData);
                }
            }
        }

        private string ParseSunriseTime(string apiResponse)
        {
            try
            {
                var jsonResult = Newtonsoft.Json.JsonConvert.DeserializeObject<dynamic>(apiResponse);
                var sunrise = jsonResult?.results.sunrise;
                DateTime sunriseDateTime = DateTime.MinValue;

                if (sunrise != null && DateTime.TryParse((string)sunrise, out sunriseDateTime))
                {
                    return sunriseDateTime.ToString("HH:mm:ss");
                }
                else
                {
                    return "Error parsing sunrise date and time";
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error parsing JSON: {ex.Message}");
                throw;
            }
        }

        private string ParseSunsetTime(string apiResponse)
        {
            try
            {
                var jsonResult = Newtonsoft.Json.JsonConvert.DeserializeObject<dynamic>(apiResponse);
                var sunset = jsonResult?.results.sunset;
                DateTime sunsetDateTime = DateTime.MinValue;

                if (sunset != null && DateTime.TryParse((string)sunset, out sunsetDateTime))
                {
                    return sunsetDateTime.ToString("HH:mm:ss");
                }
                else
                {
                    return "Error parsing sunrise date and time";
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error parsing JSON: {ex.Message}");
                throw;
            }
        }

        private string ParseDayLength(string apiResponseToday, string apiResponseYesterday, string apiResponseShortestDay)
        {
            try
            {
                var jsonResultToday = Newtonsoft.Json.JsonConvert.DeserializeObject<dynamic>(apiResponseToday);
                var jsonResultYesterday = Newtonsoft.Json.JsonConvert.DeserializeObject<dynamic>(apiResponseYesterday);
                var jsonResultShortestDay = Newtonsoft.Json.JsonConvert.DeserializeObject<dynamic>(apiResponseShortestDay);

                var dayLengthSecondsToday = jsonResultToday?.results.day_length;
                var dayLengthSecondsYesterday = jsonResultYesterday?.results.day_length;
                var dayLengthSecondsShortestDay = jsonResultShortestDay?.results.day_length;

                long dayLengthDifference = dayLengthSecondsToday - dayLengthSecondsYesterday;
                long shortestDayLengthDifference = dayLengthSecondsToday - dayLengthSecondsShortestDay;

                if (dayLengthSecondsToday != null && dayLengthSecondsYesterday != null)
                {
                    TimeSpan dayLengthTodayTimeSpan = TimeSpan.FromSeconds((long)dayLengthSecondsToday);
                    TimeSpan dayLengthDifferenceTimeSpan = TimeSpan.FromSeconds(dayLengthDifference);
                    TimeSpan shortestDayLengthDifferenceTimeSpan = TimeSpan.FromSeconds(shortestDayLengthDifference);

                    int hoursToday = dayLengthTodayTimeSpan.Hours;
                    int minutesToday = dayLengthTodayTimeSpan.Minutes;
                    int secondsToday = dayLengthTodayTimeSpan.Seconds;

                    int hoursYesterday = dayLengthDifferenceTimeSpan.Hours;
                    int minutesYesterday = dayLengthDifferenceTimeSpan.Minutes;
                    int secondsYesterday = dayLengthDifferenceTimeSpan.Seconds;
                    
                    int hoursFromShortestDay = shortestDayLengthDifferenceTimeSpan.Hours;
                    int minutesFromShortestDay = shortestDayLengthDifferenceTimeSpan.Minutes;
                    int secondsFromShortestDay = shortestDayLengthDifferenceTimeSpan.Seconds;



                    string formattedDayLength = $"{hoursToday:D2}:{minutesToday:D2}:{secondsToday:D2}" +
                        $"\nThe difference between yesterday and today: {hoursYesterday:D2}:{minutesYesterday:D2}:{secondsYesterday:D2}" +
                        $"\nThe difference between today and the shortest day (22.12.2023): {hoursFromShortestDay:D2}:{minutesFromShortestDay:D2}:{secondsFromShortestDay:D2}";
                    return formattedDayLength;
                }
                else
                {
                    return "Error: dayLengthSeconds is null";
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error parsing JSON: {ex.Message}");
                throw;
            }
        }
    }
}
