using System;
using System.Threading.Tasks;
using System;
using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;


namespace SunTgBot
{
    class BotManager
    {
        private readonly TelegramBotClient botClient;
        private readonly long chatId;
        private DateTime nextMidnight;
        private readonly string botToken;
        private Timer timer;
        public BotManager(string botToken, long chatId)
        {
            this.botClient = new TelegramBotClient(botToken);
            this.chatId = chatId;
            this.botToken = botToken;
        }

        public async Task StartBot()
        {
            Console.WriteLine("Bot is starting...");

            DateTime targetTime = new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day, 12, 00, 0);

            TimeSpan initialDelay = GetTimeUntil(targetTime);

            var timer = new Timer(async state => await Program.HandleGetTodaysInfo(chatId, botToken), null, initialDelay, TimeSpan.FromDays(1));

            await ListenForMessagesAsync();

            Console.WriteLine("Press any key to exit.");

            Console.ReadKey();

            timer.Change(Timeout.Infinite, Timeout.Infinite);
            timer.Dispose();

            Console.WriteLine("Bot is stopping...");
        }

        private TimeSpan GetTimeUntil(DateTime targetTime)
        {
            DateTime now = DateTime.Now;
            DateTime nextExecution = new DateTime(now.Year, now.Month, now.Day, targetTime.Hour, targetTime.Minute, targetTime.Second);

            if (now > nextExecution)
            {
                nextExecution = nextExecution.AddDays(1);
            }

            TimeSpan timeUntilNextExecution = nextExecution - now;
            return timeUntilNextExecution;
        }


        public async Task ListenForMessagesAsync()
        {
            using var cts = new CancellationTokenSource();

            var receiverOptions = new ReceiverOptions
            {
                AllowedUpdates = Array.Empty<UpdateType>() 
            };
            botClient.StartReceiving(
                updateHandler: HandleUpdateAsync,
                pollingErrorHandler: HandlePollingErrorAsync,
                receiverOptions: receiverOptions,
                cancellationToken: cts.Token
            );
        }

        private async Task HandleUpdateAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
        {
            if (update.Message is not { } message)
            {
                return;
            }

            if (message?.Text != null && message.Text.StartsWith("/gettodaysinfo"))
            {
                await Program.HandleGetTodaysInfo(chatId, botToken);
            }
        }

        private Task HandlePollingErrorAsync(ITelegramBotClient botClient, Exception exception, CancellationToken cancellationToken)
        {
            var ErrorMessage = exception switch
            {
                ApiRequestException apiRequestException
                    => $"Telegram API Error:\n[{apiRequestException.ErrorCode}]\n{apiRequestException.Message}",
                _ => exception.ToString()
            };

            Console.WriteLine(ErrorMessage);
            return Task.CompletedTask;
        }
    }
}
