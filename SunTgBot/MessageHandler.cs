using Telegram.Bot.Polling;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types;
using Telegram.Bot;
using Telegram.Bot.Exceptions;

namespace DayIncrease;

internal class MessageHandler
{
    private readonly WeatherApiManager _weatherApiManager;
    private readonly LocationService _locationService;
    private readonly TelegramBotClient _botClient;
    private readonly UpdateScheduler _updateScheduler;
    private long _chatId;
    private readonly Dictionary<long, string> _lastCommands = new();

    internal MessageHandler(WeatherApiManager weatherApiManager, LocationService locationService,
        TelegramBotClient botClient, UpdateScheduler updateScheduler)
    {
        _weatherApiManager = weatherApiManager;
        _locationService = locationService;
        _botClient = botClient;
        _updateScheduler = updateScheduler;
    }

    public async Task ListenForMessagesAsync(CancellationToken cancellationToken)
    {
        var receiverOptions = new ReceiverOptions
        {
            AllowedUpdates = [UpdateType.Message]
        };

        _botClient.StartReceiving(
            HandleUpdateAsync,
            HandlePollingErrorAsync,
            receiverOptions,
            cancellationToken
        );

        try
        {
            await Task.WhenAny(Task.Delay(Timeout.Infinite, cancellationToken));
        }
        catch (TaskCanceledException)
        {
            Console.WriteLine("Bot receiving has been cancelled.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"An error occurred: {ex.Message}");
        }
    }

    private async Task HandleGetTodaysInfo(long chatId)
    {
        var date = DateTime.Now.Date.ToLocalTime();

        var weatherDataJson = await _weatherApiManager.GetTimeAsync(date);

        if (!string.IsNullOrEmpty(weatherDataJson))
        {
            var weatherData = Newtonsoft.Json.JsonConvert.DeserializeObject<WeatherData>(weatherDataJson);

            if (weatherData != null)
            {
                var sunriseTimeString = weatherData.SunriseTime ?? "N/A";
                var sunsetTimeString = weatherData.SunsetTime ?? "N/A";
                var dayLengthString = weatherData.DayLength ?? "N/A";

                await _botClient.SendMessage(chatId, $"Sunrise time: {sunriseTimeString}" +
                                                     $"\nSunset time: {sunsetTimeString}" +
                                                     $"\nThe day length: {dayLengthString}");
            }
            else
            {
                await _botClient.SendMessage(chatId, "Unable to retrieve weather data.");
            }
        }
        else
        {
            Console.WriteLine("Error fetching weather data");
        }
    }

    private async Task HandleDaylightInfoAsync(long chatId)
    {
        if (_weatherApiManager is { Latitude: <= 0, Longitude: <= 0 })
        {
            var chat = await _botClient.GetChat(chatId);
            if (chat.Type == ChatType.Private)
                await _locationService.RequestLocationAsync(chatId, CancellationToken.None);
            else
                await _botClient.SendMessage(chatId, "Please send your location to proceed.");
            return;
        }

        await _updateScheduler.SendDailyMessageAsync();

        if (_updateScheduler.IsDaylightIncreasing)
            await HandleGetTodaysInfo(chatId);
        else
            await _botClient.SendMessage(chatId, "Daylight hours are shortening, wait for the winter solstice.");
    }

    private async Task HandleUpdateAsync(ITelegramBotClient botClient, Update update,
        CancellationToken cancellationToken)
    {
        try
        {
            if (update.Message is not { } message) return;

            _chatId = message.Chat.Id;

            if (message is { Type: MessageType.Text, Text: not null })
            {
                var command = message.Text.Split(' ')[0];
                var atIndex = command.IndexOf('@');

                if (atIndex >= 0) command = command[..atIndex];

                switch (command)
                {
                    case "/start":
                        await botClient.SendMessage(_chatId, "Bot started! Use available commands to interact.",
                            cancellationToken: cancellationToken);
                        break;
                    case "/gettodaysinfo":
                        if (_locationService.IsLocationReceived)
                        {
                            await HandleDaylightInfoAsync(_chatId);
                        }
                        else
                        {
                            _lastCommands[_chatId] = command;
                            _locationService.OnLocationReceived = async () =>
                            {
                                if (_lastCommands.TryGetValue(_chatId, out var savedCommand) &&
                                    savedCommand == "/gettodaysinfo")
                                {
                                    _lastCommands.Remove(_chatId);
                                    await HandleDaylightInfoAsync(_chatId);
                                }
                            };

                            await _locationService.RequestLocationAsync(_chatId, cancellationToken);
                        }

                        break;

                    case "/changelocation":
                        _lastCommands[_chatId] = command;
                        _locationService.OnLocationReceived = () =>
                        {
                            if (_lastCommands.TryGetValue(_chatId, out var savedCommand) &&
                                savedCommand == "/changelocation") _lastCommands.Remove(_chatId);
                            return Task.CompletedTask;
                        };

                        await _locationService.RequestLocationAsync(_chatId, cancellationToken);
                        break;

                    case "/getdaystillsolstice":
                        await _updateScheduler.HandleDaysTillSolsticeAsync(_chatId);
                        break;
                    case "/setintervals":
                        if (_locationService.IsLocationReceived)
                        {
                            _updateScheduler.ScheduleUpdates(async () => await HandleDaylightInfoAsync(_chatId));
                            await botClient.SendMessage(_chatId,
                                "The next message will be sent after 24 hours. You will receive messages every day until the summer solstice.",
                                cancellationToken: cancellationToken);
                        }
                        else
                        {
                            _lastCommands[_chatId] = command;
                            _locationService.OnLocationReceived = async () =>
                            {
                                if (_lastCommands.TryGetValue(_chatId, out var savedCommand) &&
                                    savedCommand == "/setintervals")
                                {
                                    _lastCommands.Remove(_chatId);
                                    _updateScheduler.ScheduleUpdates(async () => await HandleDaylightInfoAsync(_chatId));
                                    await botClient.SendMessage(_chatId,
                                        "The next message will be sent after 24 hours. You will receive messages every day until the summer solstice.",
                                        cancellationToken: cancellationToken);
                                }
                            };

                            await _locationService.RequestLocationAsync(_chatId, cancellationToken);
                        }

                        break;
                    case "/cancelintervals":
                        _updateScheduler.CancelUpdates(_chatId, cancellationToken);
                        await botClient.SendMessage(_chatId, "Intervals cancelled.",
                            cancellationToken: cancellationToken);
                        break;
                }
            }

            if (message.Type == MessageType.Location)
            {
                await _locationService.HandleLocationReceivedAsync(message, cancellationToken);

                if (_locationService.IsLocationReceived) _locationService.IsLocationReceived = false;
            }
        }
        catch (ApiRequestException apiEx) when (apiEx.ErrorCode == 400 && apiEx.Message.Contains("query is too old"))
        {
            Console.WriteLine($"API request error: {apiEx.Message}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Unhandled exception in HandleUpdateAsync: {ex.Message}");
        }
    }

    private static Task HandlePollingErrorAsync(ITelegramBotClient botClient, Exception exception,
        CancellationToken cancellationToken)
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