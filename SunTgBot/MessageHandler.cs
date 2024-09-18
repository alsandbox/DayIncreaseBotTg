using Telegram.Bot.Polling;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types;
using Telegram.Bot;
using Telegram.Bot.Exceptions;

namespace SunTgBot
{
    internal class MessageHandler
    {
        private bool isDaylightIncreasing;
        private readonly string botToken;
        private readonly WeatherApiManager weatherApiManager;
        private readonly TelegramBotClient botClient;

        internal MessageHandler(string botToken, WeatherApiManager weatherApiManager, TelegramBotClient botClient)
        {
            this.botToken = botToken;
            this.weatherApiManager = weatherApiManager;
            this.botClient = botClient;
        }

        public async Task SendDailyMessageAsync()
        {
            var today = DateTime.Now.Date;
            var solsticeStatus = GetSolsticeStatus(today);

            if (solsticeStatus.isSolsticeDay)
            {
                await Console.Out.WriteLineAsync($"It's the {solsticeStatus.solsticeType} solstice.");
            }

            isDaylightIncreasing = solsticeStatus.isDaylightIncreasing;
        }

        private static (bool isSolsticeDay, string solsticeType, bool isDaylightIncreasing) GetSolsticeStatus(DateTime currentDate)
        {
            DateTime winterSolstice = new DateTime(currentDate.Year, 12, 21, 0, 0, 0, DateTimeKind.Local);
            DateTime summerSolstice = new DateTime(currentDate.Year, 6, 21, 0, 0, 0, DateTimeKind.Local);

            bool isSolsticeDay = false;
            string solsticeType = string.Empty;
            bool isDayIncreasing = false;

            if (currentDate == winterSolstice)
            {
                isSolsticeDay = true;
                solsticeType = "winter";
                winterSolstice = winterSolstice.AddYears(1);
            }
            else if (currentDate == summerSolstice)
            {
                isSolsticeDay = true;
                solsticeType = "summer";
                summerSolstice = summerSolstice.AddYears(1);
            }

            isDayIncreasing = currentDate >= winterSolstice && currentDate < summerSolstice;
            return (isSolsticeDay, solsticeType, isDayIncreasing);
        }

        public async Task ListenForMessagesAsync(CancellationToken cancellationToken)
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

        public async Task HandleUpdateAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
        {
            if (update.Message?.Text?.StartsWith("/gettodaysinfo") == true)
            {
                await SendDailyMessageAsync();
                long chatId = update.Message.Chat.Id;

                if (isDaylightIncreasing)
                {
                    await Program.HandleGetTodaysInfo(chatId, botToken, weatherApiManager);
                }
                else
                {
                    await botClient.SendTextMessageAsync(chatId, "Daylight hours are shortening, wait for the next solstice.");
                }
            }
        }

        public static Task HandlePollingErrorAsync(ITelegramBotClient botClient, Exception exception, CancellationToken cancellationToken)
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
