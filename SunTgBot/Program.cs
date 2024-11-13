using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Telegram.Bot;

namespace SunTgBot
{
    internal static class Program
    {
        static async Task Main(string[] args)
        {
            Console.WriteLine("Reading bot token...");
            //string botToken = GetTokenFromArgsOrEnv(args, "BOT_TOKEN");
            string botToken = "6580886307:AAH5p2YJUkf3v8CWtODopMTz3oZ67zDAkcY";

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

        static string GetTokenFromArgsOrEnv(string[] args, string envVarName)
        {
            string? token = args.Length > 0 ? args[0] : Environment.GetEnvironmentVariable(envVarName);

            if (string.IsNullOrEmpty(token))
            {
                throw new ArgumentException($"The bot token must be provided via command-line arguments or the {envVarName} environment variable.");
            }
            return token;
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

