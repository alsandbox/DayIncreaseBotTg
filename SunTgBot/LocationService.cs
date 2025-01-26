using Telegram.Bot.Types.ReplyMarkups;
using Telegram.Bot.Types;
using Telegram.Bot;

namespace DayIncrease
{
    internal class LocationService
    {
        public bool IsLocationReceived { get; set; }
        private readonly TelegramBotClient botClient;
        private readonly WeatherApiManager api;
        public Func<Task>? OnLocationReceived { get; set; }

        internal LocationService(TelegramBotClient botClient, WeatherApiManager api)
        {
            this.botClient = botClient;
            this.api = api;
        }

        public async Task RequestLocationAsync(long chatId, CancellationToken cancellationToken)
        {
            var replyKeyboard = new ReplyKeyboardMarkup(new[]
            {
                new KeyboardButton("Send Location")
                {
                    RequestLocation = true
                }
            })
            {
                ResizeKeyboard = true,
                OneTimeKeyboard = true
            };

            await botClient.SendMessage(
                chatId: chatId,
                text: "To receive the info, please share your location:",
                replyMarkup: replyKeyboard,
                cancellationToken: cancellationToken
            );
        }

        public async Task HandleLocationReceivedAsync(Message message, CancellationToken cancellationToken)
        {
            var location = message.Location;

            if (location is null || (location.Latitude <= 0 && location.Longitude <= 0))
            {
                await botClient.SendMessage(
                    chatId: message.Chat.Id,
                    text: "Invalid location received. Please try again.",
                    cancellationToken: cancellationToken
                );

                await RequestLocationAsync(message.Chat.Id, cancellationToken);
                return;
            }

            IsLocationReceived = true;
            api.Latitude = location.Latitude;
            api.Longitude = location.Longitude;

            await botClient.SendMessage(
                chatId: message.Chat.Id,
                text: "Location received. You can now start receiving information.",
                replyMarkup: new ReplyKeyboardRemove(),
                cancellationToken: cancellationToken
            );

            if (OnLocationReceived != null)
            {
                var callback = OnLocationReceived;
                OnLocationReceived = null;
                await callback.Invoke();
            }
        }
    }
}
