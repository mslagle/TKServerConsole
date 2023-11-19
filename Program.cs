using System.Net;
using System.Diagnostics;
using System.Globalization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using TKServerConsole.Managers;
using TKServerConsole.Configuration;
using TKServerConsole.Repositories;

namespace TKServerConsole
{
    public class Program
    {
        private static string[] logo = new string[]
        {
            @"    _____ ___   _   __  __ _  _____ ___ _____ ",
            @"   |_   _| __| /_\ |  \/  | |/ /_ _/ __|_   _|",
            @"     | | | _| / _ \| |\/| | ' < | |\__ \ | |  ",
            @"     |_| |___/_/ \_\_|  |_|_|\_\___|___/ |_|  ",
        };

        public static async Task Main(string[] args)
        {
            if (Debugger.IsAttached) { CultureInfo.DefaultThreadCurrentUICulture = CultureInfo.GetCultureInfo("en-US"); }

            Console.WriteLine("");
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine(logo[0]);
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine(logo[1]);
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine(logo[2]);
            Console.ForegroundColor = ConsoleColor.Blue;
            Console.WriteLine(logo[3]);
            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine("");
            Console.WriteLine("");

            var configuration = new ConfigurationBuilder()
            .AddJsonFile("appsettings.json")
            .Build();

            var services = new ServiceCollection();
            services.AddLogging(builder =>
            {
                builder.AddConfiguration(configuration.GetSection("Logging"));
                builder.AddSimpleConsole(options =>
                {
                    options.IncludeScopes = false;
                    options.SingleLine = true;
                    options.TimestampFormat = "HH:mm:ss";
                });
                builder.AddFile(o =>
                {
                    o.RootPath = AppContext.BaseDirectory;
                });
            });

            services.AddSingleton<IConfiguration>(configuration);
            services.AddSingleton<TeamkistConfiguration>();
            services.AddSingleton<ISaveRepository, FileSaveRepository>();
            services.AddSingleton<TeamkistManager>();
            services.AddSingleton<TKPlayerManager>();
            services.AddSingleton<TeamkistServer>();
            services.AddSingleton<TKEditorState>();

            await using (var sp = services.BuildServiceProvider())
            {
                ILogger<Program> logger = sp.GetService<ILoggerFactory>().CreateLogger<Program>();

                TeamkistConfiguration teamkistConfiguration = sp.GetService<TeamkistConfiguration>();
                teamkistConfiguration.LogConfiguration();

                TeamkistManager manager = sp.GetService<TeamkistManager>();
                manager.Instantiate();
                manager.Run();
            }
        }
    }
}
