using Telegram.Bot;

namespace DayIncrease;

internal class BotManager
{
    private readonly MessageHandler _messageHandler;
    private readonly CancellationTokenSource _cts = new();
    public BotManager(string botToken, WeatherApiManager weatherApiManager)
    {
        var botClient = new TelegramBotClient(botToken);
        var locationService = new LocationService(botClient, weatherApiManager);
        var updateScheduler = new UpdateScheduler(botClient);
        _messageHandler = new MessageHandler(weatherApiManager, locationService, botClient, updateScheduler);
    }

    public async Task StartBotAsync()
    {
        Console.CancelKeyPress += (s, e) =>
        {
            Console.WriteLine("Ctrl+C pressed!");
            e.Cancel = true;
            _cts.Cancel();
        };

        Console.WriteLine("Bot is starting...");
        await _messageHandler.ListenForMessagesAsync(_cts.Token);
        Console.WriteLine("Bot is stopping...");
    }
}