using Newtonsoft.Json;
using System.Globalization;

namespace DayIncrease;

internal static class WeatherDataParser
{
    public static string ParseSunriseTime(string apiResponse)
    {
        try
        {
            var jsonResult = JsonConvert.DeserializeObject<dynamic>(apiResponse);
            var sunrise = jsonResult?.results.sunrise;

            if (sunrise is null || !DateTimeOffset.TryParse((string)sunrise,
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.None,
                    out var sunriseDateTimeOffset)) return "Error parsing sunrise time";
            var desiredTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Central European Standard Time");
            var localSunriseTime = TimeZoneInfo.ConvertTime(sunriseDateTimeOffset.UtcDateTime, TimeZoneInfo.Utc,
                desiredTimeZone);
            return localSunriseTime.ToString("HH:mm:ss");

        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error parsing sunrise time: {ex.Message}");
            throw;
        }
    }

    public static string ParseSunsetTime(string apiResponse)
    {
        try
        {
            var jsonResult = JsonConvert.DeserializeObject<dynamic>(apiResponse);
            var sunset = jsonResult?.results.sunset;

            if (sunset is not null && DateTimeOffset.TryParse((string)sunset,
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.None,
                    out var sunsetDateTimeOffset))
            {
                var desiredTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Central European Standard Time");
                var localSunsetTime =
                    TimeZoneInfo.ConvertTime(sunsetDateTimeOffset.UtcDateTime, TimeZoneInfo.Utc, desiredTimeZone);
                return localSunsetTime.ToString("HH:mm:ss");
            }

            return "Error parsing sunset time";
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error parsing sunset time: {ex.Message}");
            throw;
        }
    }

    public static string ParseDayLength(string apiResponseToday, string apiResponseYesterday,
        string apiResponseShortestDay)
    {
        try
        {
            var jsonResultToday = JsonConvert.DeserializeObject<dynamic>(apiResponseToday);
            var jsonResultYesterday = JsonConvert.DeserializeObject<dynamic>(apiResponseYesterday);
            var jsonResultShortestDay = JsonConvert.DeserializeObject<dynamic>(apiResponseShortestDay);

            var dayLengthSecondsToday = (long?)jsonResultToday?.results.day_length;
            var dayLengthSecondsYesterday = (long?)jsonResultYesterday?.results.day_length;
            var dayLengthSecondsShortestDay = (long?)jsonResultShortestDay?.results.day_length;

            if (dayLengthSecondsToday != null && dayLengthSecondsYesterday != null &&
                dayLengthSecondsShortestDay != null)
                return CalculateDayLength(dayLengthSecondsToday.Value, dayLengthSecondsYesterday.Value,
                    dayLengthSecondsShortestDay.Value);

            return "Error: dayLengthSeconds is null";
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error parsing day length: {ex.Message}");
            throw;
        }
    }

    private static string CalculateDayLength(long todayLength, long yesterdayLength, long shortestDayLength)
    {
        var dayLengthTodayTimeSpan = TimeSpan.FromSeconds(todayLength);
        var dayLengthDifferenceTimeSpan = TimeSpan.FromSeconds(todayLength - yesterdayLength);
        var shortestDayLengthDifferenceTimeSpan = TimeSpan.FromSeconds(todayLength - shortestDayLength);

        var formattedDayLength = $@"{dayLengthTodayTimeSpan:hh\:mm\:ss}" +
                                 $"\nThe difference between yesterday and today: {dayLengthDifferenceTimeSpan:hh\\:mm\\:ss}" +
                                 $"\nThe difference between today and the shortest day: {shortestDayLengthDifferenceTimeSpan:hh\\:mm\\:ss}";

        return formattedDayLength;
    }

    internal static int CalculateDaysTillNearestSolstice(DateTime today)
    {
        TimeSpan date = default;
        var solstice = SolsticeData.GetSolsticeByYear(today.Year);

        if (today.Month <= 7)
        {
            if (solstice != null) date = solstice.Value.Summer - today;
        }
        else if (solstice != null) date = solstice.Value.Winter - today;

        return date.Days;
    }
}