using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;


namespace SunTgBot
{
    class BotManager(string botToken, long chatId)
    {
        private readonly TelegramBotClient botClient = new TelegramBotClient(botToken);
        private readonly long chatId = chatId;
        private readonly string botToken = botToken;

        public async Task StartBot()
        {
            Console.WriteLine("Bot is starting...");

            DateTime targetTime = new (DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day, 18, 05, 0, DateTimeKind.Utc);

            TimeSpan initialDelay = GetTimeUntil(targetTime);

            var timer = new Timer(async state => await Program.HandleGetTodaysInfo(chatId, botToken), null, initialDelay, TimeSpan.FromDays(1));

            await ListenForMessagesAsync();

            Console.WriteLine("Bot is stopping...");
        }

        private TimeSpan GetTimeUntil(DateTime targetTime)
        {
            DateTime now = DateTime.Now;
            DateTime nextExecution = new (now.Year, now.Month, now.Day, targetTime.Hour, targetTime.Minute, targetTime.Second, DateTimeKind.Utc);

            if (now > nextExecution)
            {
                nextExecution = nextExecution.AddDays(1);
            }

            TimeSpan timeUntilNextExecution = nextExecution - now;
            return timeUntilNextExecution;
        }

        private async Task ListenForMessagesAsync()
        {
            var receiverOptions = new ReceiverOptions
            {
                AllowedUpdates = new[] { UpdateType.Message }
            };

            botClient.StartReceiving(
                updateHandler: HandleUpdateAsync,
                pollingErrorHandler: HandlePollingErrorAsync,
                receiverOptions: receiverOptions
            );

            await RunBackgroundService();
        }

        private async Task RunBackgroundService()
        {
            DateTime endDate = new DateTime(2024, 6, 21);

            while (DateTime.Now.Date <= endDate)
            {
                await Program.HandleGetTodaysInfo(chatId, botToken);

                await Task.Delay(TimeSpan.FromDays(1));
            }

            Console.WriteLine("Daily tasks completed. Stopping the background service.");
        }

        private async Task HandleUpdateAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
        {
            if (update.Message is not { } message)
            {
                return;
            }

            if (message.Text != null && message.Text.StartsWith("/gettodaysinfo"))
            {
                await Program.HandleGetTodaysInfo(chatId, botToken);
            }
        }

        private Task HandlePollingErrorAsync(ITelegramBotClient botClient, Exception exception, CancellationToken cancellationToken)
        {
            var errorMessage = exception switch
            {
                ApiRequestException apiRequestException
                    => $"Telegram API Error:\n[{apiRequestException.ErrorCode}]\n{apiRequestException.Message}",
                _ => exception.ToString()
            };

            Console.WriteLine(errorMessage);
            return Task.CompletedTask;
        }
    }
}