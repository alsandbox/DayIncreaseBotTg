using Telegram.Bot;

namespace DayIncrease;

internal sealed class UpdateScheduler : IDisposable
{
    internal bool IsDaylightIncreasing;
    private readonly ITelegramBotClient _botClient;
    private Timer? _timer;
    private bool _isDisposed;

    internal UpdateScheduler(ITelegramBotClient botClient)
    {
        _botClient = botClient;
    }

    internal void ScheduleUpdates(Func<Task> handleDaylightInfoAsync)
    {
        var interval = TimeSpan.FromDays(1);

        _timer = new Timer(async void (state) =>
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
            await Console.Out.WriteLineAsync($"It's the {solsticeStatus.solsticeType} solstice.");

        IsDaylightIncreasing = solsticeStatus.isDaylightIncreasing;
    }

    private static (bool isSolsticeDay, string solsticeType, bool isDaylightIncreasing) GetSolsticeStatus(
        DateTime currentDate)
    {
        var solstice = SolsticeData.GetSolsticeByYear(currentDate.Year);
        if (solstice == null) return (false, string.Empty, false);

        var winterSolstice = solstice.Value.Winter;
        var summerSolstice = solstice.Value.Summer;

        if (currentDate > winterSolstice)
        {
            var nextYearSolstice = SolsticeData.GetSolsticeByYear(currentDate.Year + 1);
            if (nextYearSolstice != null) summerSolstice = nextYearSolstice.Value.Summer;
        }
        else if (currentDate < summerSolstice)
        {
            var previousYearSolstice = SolsticeData.GetSolsticeByYear(currentDate.Year - 1);
            if (previousYearSolstice != null) winterSolstice = previousYearSolstice.Value.Winter;
        }

        var isSolsticeDay = currentDate == winterSolstice || currentDate == summerSolstice;
        var solsticeType = isSolsticeDay ? currentDate == winterSolstice ? "winter" : "summer" : string.Empty;
        var isDaylightIncreasing = currentDate > winterSolstice && currentDate < summerSolstice;

        return (isSolsticeDay, solsticeType, isDaylightIncreasing);
    }

    public async Task HandleDaysTillSolsticeAsync(long chatId)
    {
        await SendDailyMessageAsync();

        if (!IsDaylightIncreasing)
        {
            var today = DateTime.Now;
            await _botClient.SendMessage(chatId,
                $"Days till the solstice: {WeatherDataParser.CalculateDaysTillNearestSolstice(today)}.");
        }
        else
        {
            await _botClient.SendMessage(chatId, "Daylight hours are increasing, wait for the summer solstice.");
        }
    }

    internal void CancelUpdates(long chatId, CancellationToken cancellationToken)
    {
        _timer?.Change(Timeout.Infinite, Timeout.Infinite);

        _botClient.SendMessage(
            chatId,
            "Updates have been cancelled.",
            cancellationToken: cancellationToken
        );
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    private void Dispose(bool disposing)
    {
        if (_isDisposed)
            return;

        if (disposing) _timer?.Dispose();

        _isDisposed = true;
    }
}