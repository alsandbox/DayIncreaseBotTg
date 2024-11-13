using Telegram.Bot;
namespace SunTgBot
{
    internal class BotManager : IDisposable
    {
        private System.Threading.Timer? timer;
        private readonly CancellationTokenSource cts;
        private bool disposed;
        private readonly MessageHandler messageHandler;

        public BotManager(string botToken, WeatherApiManager weatherApiManager)
        {
            TelegramBotClient botClient = new TelegramBotClient(botToken);
            cts = new CancellationTokenSource();
            WeatherApiManager _weatherApiManager = weatherApiManager;
            LocationService locationService = new LocationService(botClient, _weatherApiManager);
            messageHandler = new MessageHandler(_weatherApiManager, locationService, botClient);
        }

        public async Task StartBotAsync()
        {
            Console.WriteLine("Bot is starting...");

            TimeSpan initialDelay = GetInitialDelay(13, 0);
            timer = new Timer(async state => await messageHandler.SendDailyMessageAsync(), null, initialDelay, TimeSpan.FromDays(1));
            await messageHandler.ListenForMessagesAsync(cts.Token);

            Console.WriteLine("Bot is stopping...");
        }

        private static TimeSpan GetInitialDelay(int targetHour, int targetMinute)
        {
            DateTime targetTime = DateTime.UtcNow.Date.AddHours(targetHour).AddMinutes(targetMinute);
            if (DateTime.UtcNow > targetTime)
                targetTime = targetTime.AddDays(1);

            return targetTime - DateTime.UtcNow;
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposed)
                return;

            if (disposing)
            {
                timer?.Dispose();
                cts.Cancel();
                cts.Dispose();
            }

            disposed = true;
        }
    }
}
