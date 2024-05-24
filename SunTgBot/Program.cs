using Telegram.Bot;

namespace SunTgBot
{
    internal static class Program
    {
        static async Task Main(string[] args)
        {
            Console.WriteLine("Reading bot token...");
            string botToken = GetTokenFromArgsOrEnv(args, "BOT_TOKEN");
            Console.WriteLine($"Bot token: {botToken}");

            Console.WriteLine("Reading chat ID...");
            long chatId = GetChatIdFromArgsOrEnv(args, "CHAT_ID");
            Console.WriteLine($"Chat ID: {chatId}");

            var botManager = new BotManager(botToken, chatId);
            await botManager.StartBot();
        }

        static string GetTokenFromArgsOrEnv(string[] args, string envVarName)
        {
            string? token = args.Length > 0 ? args[0] : Environment.GetEnvironmentVariable(envVarName);
            Console.WriteLine($"Token from args or env: {token}");
            if (string.IsNullOrEmpty(token))
            {
                throw new ArgumentException($"The bot token must be provided via command-line arguments or the {envVarName} environment variable.");
            }
            return token;
        }

        static long GetChatIdFromArgsOrEnv(string[] args, string envVarName)
        {
            string? input = args.Length > 1 ? args[1] : Environment.GetEnvironmentVariable(envVarName);
            Console.WriteLine($"Chat ID input from args or env: {input}");
            if (string.IsNullOrEmpty(input) || !long.TryParse(input, out long chatId))
            {
                throw new ArgumentException($"The chat ID must be provided via command-line arguments or the {envVarName} environment variable and must be a valid long integer.");
            }
            return chatId;
        }

        internal static async Task HandleGetTodaysInfo(long chatId, string botToken)
        {
            var weatherApiManager = new WeatherApiManager();

            TelegramBotClient botClient = new TelegramBotClient(botToken);

            DateTime date = DateTime.Now.Date.ToLocalTime();
            float latitude = 51.759050f;
            float longitude = 19.458600f;
            string tzId = "Europe/Warsaw";

            string weatherDataJson = await weatherApiManager.GetTimeAsync(latitude, longitude, date, tzId);

            if (!string.IsNullOrEmpty(weatherDataJson))
            {
                WeatherData? weatherData = Newtonsoft.Json.JsonConvert.DeserializeObject<WeatherData>(weatherDataJson);

                if (weatherData != null)
                {
                    string sunriseTimeString = weatherData.SunriseTime?.ToString() ?? "N/A";
                    string sunsetTimeString = weatherData.SunsetTime?.ToString() ?? "N/A";
                    string dayLengthString = weatherData.DayLength?.ToString() ?? "N/A";

                    await botClient.SendTextMessageAsync(chatId, $"Sunrise time: {sunriseTimeString}" +
                        $"\nSunset time: {sunsetTimeString}" +
                        $"\nThe day length: {dayLengthString}");
                }
                else
                {
                    await botClient.SendTextMessageAsync(chatId, "Unable to retrieve weather data.");
                }
            }
            else
            {
                Console.WriteLine("Error fetching weather data");
            }
        }
    }
}

