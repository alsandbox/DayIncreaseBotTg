using Telegram.Bot;
using Telegram.Bot.Args;
using Telegram.Bot.Types;

namespace SunTgBot
{
    internal static class Program
    {
        static async Task Main()
        {
            string botToken = "6580886307:AAEJZmE49_uDy4tgw1qGBuPilJB-K-Fz3N4";
            long chatId = -1002142278404;
            var botManager = new BotManager(botToken, chatId);

            botManager.StartBot();
        }

        internal static async Task HandleGetTodaysInfo(long chatId, string botToken)
        {
            var weatherApiManager = new WeatherApiManager();

            TelegramBotClient botClient = new TelegramBotClient(botToken);


            DateTime date = DateTime.Now.Date.ToLocalTime();
            float latitude = 51.759050f;
            float longitude = 19.458600f;
            string tzId = "UTC+1 CET";

            string weatherDataJson = await weatherApiManager.GetTimeAsync(latitude, longitude, date, tzId);

            if (!string.IsNullOrEmpty(weatherDataJson))
            {
                WeatherData ? weatherData = Newtonsoft.Json.JsonConvert.DeserializeObject<WeatherData>(weatherDataJson);

                string sunriseTimeString = weatherData.SunriseTime.ToString();
                string sunsetTimeString = weatherData.SunsetTime.ToString();
                string dayLengthString = weatherData.DayLength.ToString();

                Console.WriteLine($"Sunrise time: {sunriseTimeString}");
                Console.WriteLine($"Sunset time: {sunsetTimeString}");
                Console.WriteLine($"The day length: {dayLengthString}");

                await botClient.SendTextMessageAsync(chatId, $"Sunrise time: {sunriseTimeString}" +
                    $"\nSunset time: {sunsetTimeString}" +
                    $"\nThe day length: {dayLengthString}");
            }
            else
            {
                Console.WriteLine("Error fetching weather data");
            }
        }
    }
}

