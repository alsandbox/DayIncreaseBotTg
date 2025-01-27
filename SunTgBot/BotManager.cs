using Telegram.Bot;

using UVindexTGBot;
namespace DayIncrease
{
    internal class BotManager
    {
        private readonly CancellationTokenSource cts;
        private readonly MessageHandler messageHandler;

        public BotManager(string botToken, WeatherApiManager weatherApiManager)
        {
            TelegramBotClient botClient = new TelegramBotClient(botToken);
            WeatherApiManager _weatherApiManager = weatherApiManager;
            LocationService locationService = new LocationService(botClient, _weatherApiManager);
            UvUpdateScheduler uvUpdateScheduler = new UvUpdateScheduler(botClient, _weatherApiManager);
            messageHandler = new MessageHandler(_weatherApiManager, locationService, botClient, uvUpdateScheduler);
        }

        public async Task StartBotAsync()
        {
            Console.WriteLine("Bot is starting...");

            await messageHandler.ListenForMessagesAsync(cts.Token);

            Console.WriteLine("Bot is stopping...");
        }
    }
}
