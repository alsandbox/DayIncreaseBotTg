using Telegram.Bot.Types.ReplyMarkups;
using Telegram.Bot.Types;
using Telegram.Bot;

namespace DayIncrease;

internal class LocationService
{
    public bool IsLocationReceived { get; set; }
    private readonly TelegramBotClient _botClient;
    private readonly WeatherApiManager _api;
    public Func<Task>? OnLocationReceived { get; set; }

    internal LocationService(TelegramBotClient botClient, WeatherApiManager api)
    {
        _botClient = botClient;
        _api = api;
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

        await _botClient.SendMessage(
            chatId,
            "To receive the info, please share your location:",
            replyMarkup: replyKeyboard,
            cancellationToken: cancellationToken
        );
    }

    public async Task HandleLocationReceivedAsync(Message message, CancellationToken cancellationToken)
    {
        var location = message.Location;

        if (location is null or { Latitude: <= 0, Longitude: <= 0 })
        {
            await _botClient.SendMessage(
                message.Chat.Id,
                "Invalid location received. Please try again.",
                cancellationToken: cancellationToken
            );

            await RequestLocationAsync(message.Chat.Id, cancellationToken);
            return;
        }

        IsLocationReceived = true;
        _api.Latitude = location.Latitude;
        _api.Longitude = location.Longitude;

        await _botClient.SendMessage(
            message.Chat.Id,
            "Location received. You can now start receiving information.",
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