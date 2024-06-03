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
    internal class BotManager : IDisposable
    {
        private System.Threading.Timer? timer;
        private readonly TelegramBotClient botClient;
        private readonly long chatId;
        private readonly string botToken;
        private readonly WeatherApiManager weatherApiManager;
        private readonly CancellationTokenSource cts;
        private bool disposed;

        public BotManager(string botToken, long chatId, WeatherApiManager weatherApiManager)
        {
            this.botToken = botToken;
            this.chatId = chatId;
            this.weatherApiManager = weatherApiManager;
            this.botClient = new TelegramBotClient(botToken);
            cts = new CancellationTokenSource();
        }

        public async Task StartBot()
        {
            Console.WriteLine("Bot is starting...");

            DateTime targetTime = DateTime.UtcNow.Date.AddHours(13).AddMinutes(00);
            TimeSpan initialDelay = GetTimeUntil(targetTime);

            timer = new Timer(async state => await SendDailyMessage(), null, initialDelay, TimeSpan.FromDays(1));

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
            await Program.HandleGetTodaysInfo(chatId, botToken, weatherApiManager);
        }

        private async Task ListenForMessagesAsync(CancellationToken cancellationToken)
        {
            var receiverOptions = new ReceiverOptions
            {
                AllowedUpdates = [UpdateType.Message]
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
                await Program.HandleGetTodaysInfo(chatId, botToken, weatherApiManager);
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
