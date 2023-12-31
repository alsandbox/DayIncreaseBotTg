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
        private readonly string botToken;

        public BotManager(string botToken, long chatId)
        {
            this.botClient = new TelegramBotClient(botToken);
            this.chatId = chatId;
            this.botToken = botToken;
        }

        public async Task StartBot()
        {
            Console.WriteLine("Bot is starting...");

            await ListenForMessagesAsync();
            await Task.Delay(TimeSpan.FromMinutes(1));
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
            await Task.Delay(TimeSpan.FromMinutes(1));
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