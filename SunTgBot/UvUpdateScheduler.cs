using DayIncrease;

using Telegram.Bot;
using Telegram.Bot.Types;

namespace UVindexTGBot
{
    internal class UvUpdateScheduler : IDisposable
    {
        internal bool isDaylightIncreasing;
        private readonly ITelegramBotClient botClient;
        private readonly WeatherApiManager apiManager;
        private Timer? timer;
        private bool disposed;

        internal long ChatId { get; set; }

        internal UvUpdateScheduler(ITelegramBotClient botClient, WeatherApiManager api)
        {
            this.botClient = botClient;
            apiManager = api;
        }

        internal void ScheduleUvUpdates(CancellationToken cancellationToken, long chatId, Func<Task> handleDaylightInfoAsync)
        {
            TimeSpan interval = TimeSpan.FromDays(1);
            
            timer = new Timer(async state =>
            {
                try
                {
                    await handleDaylightInfoAsync();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error in timer callback: {ex.Message}");
                }
            }, null, TimeSpan.Zero, interval);
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
            if (solstice == null) return (false, string.Empty, false);

            var winterSolstice = solstice.Value.Winter;
            var summerSolstice = solstice.Value.Summer;

            if (currentDate > winterSolstice)
            {
                var nextYearSolstice = SolsticeData.GetSolsticeByYear(currentDate.Year + 1);
                if (nextYearSolstice != null)
                {
                    summerSolstice = nextYearSolstice.Value.Summer;
                }
            }
            else if (currentDate < summerSolstice)
            {
                var previousYearSolstice = SolsticeData.GetSolsticeByYear(currentDate.Year - 1);
                if (previousYearSolstice != null)
                {
                    winterSolstice = previousYearSolstice.Value.Winter;
                }
            }

            bool isSolsticeDay = currentDate == winterSolstice || currentDate == summerSolstice;
            string solsticeType = isSolsticeDay ? (currentDate == winterSolstice ? "winter" : "summer") : string.Empty;
            bool isDaylightIncreasing = currentDate > winterSolstice && currentDate < summerSolstice;

            return (isSolsticeDay, solsticeType, isDaylightIncreasing);
        }

        public async Task HandleDaysTillSolsticeAsync(long chatId)
        {
            await SendDailyMessageAsync();

            if (!isDaylightIncreasing)
            {
                DateTime today = DateTime.Now;
                await botClient.SendMessage(chatId,
                    $"Days till the solstice: {WeatherDataParser.CalculateDaysTillNearestSolstice(today)}.");
            }
            else
            {
                await botClient.SendMessage(chatId, "Daylight hours are increasing, wait for the summer solstice.");
            }
        }

        internal void CancelUvUpdates(CancellationToken cancellationToken)
        {
            timer?.Change(Timeout.Infinite, Timeout.Infinite);

            botClient.SendMessage(
                chatId: ChatId,
            text: "Updates have been cancelled.",
                cancellationToken: cancellationToken
            );
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
            }

            disposed = true;
        }
    }
}