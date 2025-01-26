using Telegram.Bot.Polling;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types;
using Telegram.Bot;
using Telegram.Bot.Exceptions;
using System.Globalization;
using Telegram.Bot.Types.ReplyMarkups;
using UVindexTGBot;

namespace DayIncrease
{
    internal class MessageHandler
    {
        private readonly WeatherApiManager weatherApiManager;
        private readonly LocationService locationService;
        private readonly TelegramBotClient botClient;
        private readonly UvUpdateScheduler uvUpdateScheduler;
        private long chatId;
        private bool isAwaitingCustomIntervalInput;
        private readonly Dictionary<long, string> lastCommands = new();

        internal MessageHandler(WeatherApiManager weatherApiManager, LocationService locationService, TelegramBotClient botClient, UvUpdateScheduler uvUpdateScheduler)
        {
            this.weatherApiManager = weatherApiManager;
            this.locationService = locationService;
            this.botClient = botClient;
            this.uvUpdateScheduler = uvUpdateScheduler;
        }
 
        public async Task ListenForMessagesAsync(CancellationToken cancellationToken)
        {
            var receiverOptions = new ReceiverOptions
            {
                AllowedUpdates = [UpdateType.Message]
            };

            botClient.StartReceiving(
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
            DateTime date = DateTime.Now.Date.ToLocalTime();

            string weatherDataJson = await weatherApiManager.GetTimeAsync(date);

            if (!string.IsNullOrEmpty(weatherDataJson))
            {
                WeatherData? weatherData = Newtonsoft.Json.JsonConvert.DeserializeObject<WeatherData>(weatherDataJson);

                if (weatherData != null)
                {
                    string sunriseTimeString = weatherData.SunriseTime?.ToString() ?? "N/A";
                    string sunsetTimeString = weatherData.SunsetTime?.ToString() ?? "N/A";
                    string dayLengthString = weatherData.DayLength?.ToString() ?? "N/A";

                    await botClient.SendMessage(chatId, $"Sunrise time: {sunriseTimeString}" +
                        $"\nSunset time: {sunsetTimeString}" +
                        $"\nThe day length: {dayLengthString}");
                }
                else
                {
                    await botClient.SendMessage(chatId, "Unable to retrieve weather data.");
                }
            }
            else
            {
                Console.WriteLine("Error fetching weather data");
            }
        }

        private async Task HandleDaylightInfoAsync(long chatId)
        {
            if (weatherApiManager.Latitude <= 0 && weatherApiManager.Longitude <= 0)
            {
                var chat = await botClient.GetChat(chatId);
                if (chat.Type == ChatType.Private)
                {
                    await locationService.RequestLocationAsync(chatId, CancellationToken.None);
                }
                else
                {
                    await botClient.SendMessage(chatId, "Please send your location to proceed.");
                }
                return;
            }

            await uvUpdateScheduler.SendDailyMessageAsync();

            if (uvUpdateScheduler.isDaylightIncreasing)
            {
                await HandleGetTodaysInfo(chatId);
            }
            else
            {
                await botClient.SendMessage(chatId, "Daylight hours are shortening, wait for the winter solstice.");
            }
        }

        public async Task HandleUpdateAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
        {
            try
            {
                if (update.Message is not { } message) return;

               chatId = message.Chat.Id;

                if (message.Type == MessageType.Text && message.Text is not null)
                {
                    string command = message.Text.Split(' ')[0];
                    int atIndex = command.IndexOf('@');

                    if (atIndex >= 0)
                    {
                        command = command.Substring(0, atIndex);
                    }

                    switch (command)
                    {
                        case "/start":
                            await botClient.SendMessage(chatId, "Bot started! Use available commands to interact.");
                            break;
                        case "/gettodaysinfo":
                            if (locationService.IsLocationReceived)
                            {
                                await HandleDaylightInfoAsync(chatId);
                            }
                            else
                            {
                                lastCommands[chatId] = command;
                                locationService.OnLocationReceived = async () =>
                                {
                                    if (lastCommands.TryGetValue(chatId, out var savedCommand) && savedCommand == "/gettodaysinfo")
                                    {
                                        lastCommands.Remove(chatId);
                                        await HandleDaylightInfoAsync(chatId);
                                    }
                                };

                                await locationService.RequestLocationAsync(chatId, cancellationToken);
                            }
                            break;

                        case "/changelocation":
                            lastCommands[chatId] = command;
                            locationService.OnLocationReceived = async () =>
                            {
                                if (lastCommands.TryGetValue(chatId, out var savedCommand) && savedCommand == "/changelocation")
                                {
                                    lastCommands.Remove(chatId);
                                }
                            };

                            await locationService.RequestLocationAsync(chatId, cancellationToken);
                            break;

                        case "/getdaystillsolstice":
                            await uvUpdateScheduler.HandleDaysTillSolsticeAsync(chatId);
                            break;
                        case "/setintervals":
                            if (locationService.IsLocationReceived)
                            {
                                uvUpdateScheduler.ScheduleUvUpdates(CancellationToken.None, chatId, async () => await HandleDaylightInfoAsync(chatId));
                                await botClient.SendMessage(chatId, "The next message will be sent after 24 hours. You will receive messages every day until the summer solstice.");
                            }
                            else
                            {
                                lastCommands[chatId] = command;
                                locationService.OnLocationReceived = async () =>
                                {
                                    if (lastCommands.TryGetValue(chatId, out var savedCommand) && savedCommand == "/setintervals")
                                    {
                                        lastCommands.Remove(chatId);
                                        uvUpdateScheduler.ScheduleUvUpdates(CancellationToken.None, chatId, async () => await HandleDaylightInfoAsync(chatId));
                                        await botClient.SendMessage(chatId, "The next message will be sent after 24 hours. You will receive messages every day until the summer solstice.");
                                    }
                                };

                                await locationService.RequestLocationAsync(chatId, cancellationToken);
                            }
                            break;
                        case "/cancelintervals":
                            uvUpdateScheduler.CancelUvUpdates(cancellationToken);
                            await botClient.SendMessage(chatId, "Intervals cancelled.");
                            break;
                    }
                }

                if (message.Type == MessageType.Location)
                {
                    await locationService.HandleLocationReceivedAsync(message, cancellationToken);

                    if (locationService.IsLocationReceived)
                    {
                        locationService.IsLocationReceived = false;
                    }
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
