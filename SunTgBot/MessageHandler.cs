using Telegram.Bot.Polling;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types;
using Telegram.Bot;
using Telegram.Bot.Exceptions;

namespace DayIncrease
{
    internal class MessageHandler
    {
        private bool isDaylightIncreasing;
        private readonly WeatherApiManager weatherApiManager;
        private readonly LocationService locationService;
        private readonly TelegramBotClient botClient;

        internal MessageHandler(WeatherApiManager weatherApiManager, LocationService locationService, TelegramBotClient botClient)
        {
            this.weatherApiManager = weatherApiManager;
            this.locationService = locationService;
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
            var solstice = SolsticeData.GetSolsticeByYear(currentDate.Year);

            bool isSolsticeDay = false;
            string solsticeType = string.Empty;
            bool isDaylightIncreasing = false;

            if (solstice == null) return (isSolsticeDay, solsticeType, isDaylightIncreasing);

            if (currentDate == solstice.Value.Winter)
            {
                isSolsticeDay = true;
                solsticeType = "winter";
            }
            else if (currentDate == solstice.Value.Summer)
            {
                isSolsticeDay = true;
                solsticeType = "summer";
            }

            isDaylightIncreasing = currentDate >= solstice.Value.Winter && currentDate < solstice.Value.Summer;
            return (isSolsticeDay, solsticeType, isDaylightIncreasing);
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

                    await botClient.SendTextMessageAsync(chatId, $"Sunrise time: {sunriseTimeString}" +
                        $"\nSunset time: {sunsetTimeString}" +
                        $"\nThe day length: {dayLengthString}");
                }
                else
                {
                    await botClient.SendTextMessageAsync(chatId, "Unable to retrieve weather data.");
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
                var chat = await botClient.GetChatAsync(chatId);
                if (chat.Type == ChatType.Private)
                {
                    await locationService.RequestLocationAsync(chatId, CancellationToken.None);
                }
                else
                {
                    await botClient.SendTextMessageAsync(chatId, "Please send your location to proceed.");
                }
                return;
            }

            await SendDailyMessageAsync();

            if (isDaylightIncreasing)
            {
                await HandleGetTodaysInfo(chatId);
            }
            else
            {
                await botClient.SendTextMessageAsync(chatId, "Daylight hours are shortening, wait for the winter solstice.");
            }
        }

        private async Task HandleDaysTillSolsticeAsync(long chatId)
        {
            await SendDailyMessageAsync();

            if (!isDaylightIncreasing)
            {
                DateTime today = DateTime.Now;
                await botClient.SendTextMessageAsync(chatId, $"Days till the solstice: {WeatherDataParser.CalculateDaysTillNearestSolstice(today)}.");
            }
            else
            {
                await botClient.SendTextMessageAsync(chatId, "Daylight hours are increasing, wait for the summer solstice.");
            }
        }

        public async Task HandleUpdateAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
        {
            try
            {
                if (update.Message is not { } message) return;

                long chatId = message.Chat.Id;

                if (message.Type == MessageType.Text && message.Text is not null)
                {
                    string command = message.Text.Split(' ')[0];
                    int atIndex = command.IndexOf('@');

                    if (atIndex >= 0)
                    {
                        command = command.Substring(0, atIndex);
                    }

                    Console.WriteLine($"Received command: {command}");

                    switch (command)
                    {
                        case "/start":
                            await botClient.SendTextMessageAsync(chatId, "Bot started! Use available commands to interact.");
                            break;
                        case "/gettodaysinfo":
                            await HandleDaylightInfoAsync(chatId);
                            break;
                        case "/changelocation":
                            if (message.Chat.Type == ChatType.Private)
                            {
                                await locationService.RequestLocationAsync(chatId, cancellationToken);
                            }
                            else
                            {
                                await botClient.SendTextMessageAsync(chatId, "Please send your location to proceed.");
                            }
                            break;
                        case "/getdaystillsolstice":
                            await HandleDaysTillSolsticeAsync(chatId);
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
