using System;
using System.Threading;
using System.Threading.Tasks;
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
        private readonly string botToken;
        private CancellationTokenSource cts;

        public BotManager(string botToken, long chatId)
        {
            this.botToken = botToken;
            this.chatId = chatId;
            this.botClient = new TelegramBotClient(botToken);
            cts = new CancellationTokenSource();
        }

        public async Task StartBot()
        {
            Console.WriteLine("Bot is starting...");

            DateTime targetTime = DateTimeOffset.UtcNow.Date.AddHours(16);

            TimeSpan initialDelay = GetTimeUntil(targetTime);

            var timer = new Timer(async state => await SendDailyMessage(), null, initialDelay, TimeSpan.FromDays(1));

            await ListenForMessagesAsync(cts.Token);

            Console.WriteLine("Bot is stopping...");
        }

        private TimeSpan GetTimeUntil(DateTime targetTime)
        {
            DateTime now = DateTime.UtcNow;
            if (now > targetTime)
            {
                targetTime = targetTime.AddDays(1);
            }

            TimeSpan timeUntilNextExecution = targetTime - now;
            return timeUntilNextExecution;
        }

        private async Task SendDailyMessage()
        {
            await Program.HandleGetTodaysInfo(chatId, botToken);
        }

        private async Task ListenForMessagesAsync(CancellationToken cancellationToken)
        {
            var receiverOptions = new ReceiverOptions
            {
                AllowedUpdates = new[] { UpdateType.Message }
            };

            botClient.StartReceiving(
                updateHandler: HandleUpdateAsync,
                pollingErrorHandler: HandlePollingErrorAsync,
                receiverOptions: receiverOptions,
                cancellationToken: cancellationToken
            );

            try
            {
                await Task.Delay(Timeout.Infinite, cancellationToken);
            }
            catch (TaskCanceledException)
            {
                Console.WriteLine("Bot receiving has been cancelled.");
            }
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
