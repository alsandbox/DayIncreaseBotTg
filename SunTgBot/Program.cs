using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Telegram.Bot;

namespace DayIncrease
{
    internal static class Program
    {
        static async Task Main(string[] args)
        {
            Console.WriteLine("Reading bot token...");
            string? botToken = Environment.GetEnvironmentVariable("BOT_TOKEN");

            if (botToken is null)
            {
                throw new ArgumentNullException($"Bot token '{botToken}' is not provided.");
            }

            var botManager = ConfigureBotManager(botToken);

            if (botManager != null)
            {
                await botManager.StartBotAsync();
            }
            else
            {
                Console.WriteLine("Failed to configure BotManager. Exiting...");
            }
        }

        static BotManager? ConfigureBotManager(string botToken)
        {
            try
            {
                var serviceProvider = ConfigureServices();

                var weatherApiManager = serviceProvider.GetService<WeatherApiManager>();
                _ = serviceProvider.GetService<IConfiguration>();

                if (string.IsNullOrEmpty(botToken))
                {
                    Console.WriteLine("Bot token is not provided or configured properly.");
                    return null;
                }

                if (weatherApiManager == null)
                {
                    Console.WriteLine("WeatherApiManager is not configured properly.");
                    return null;
                }

                return new BotManager(botToken, weatherApiManager);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error configuring BotManager: {ex.Message}");
                return null;
            }
        }

        private static ServiceProvider ConfigureServices()
        {
            var builder = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);

            IConfiguration configuration = builder.Build();

            return new ServiceCollection()
                .AddSingleton<IConfiguration>(configuration)
                .AddSingleton<WeatherApiClient>()
                .AddTransient<WeatherApiManager>()
                .BuildServiceProvider();
        }
    }
}

