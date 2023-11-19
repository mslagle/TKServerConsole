using System.Net;
using System.Diagnostics;
using System.Globalization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using TKServerConsole.Managers;
using TKServerConsole.Configuration;

namespace TKServerConsole
{
    public class Program
    {
        //Default Settings.
        private static readonly IPAddress DEFAULT_IP = IPAddress.Parse((string)"0.0.0.0");
        private static readonly int DEFAULT_PORT = 50000;
        private static readonly string DEFAULT_LEVEL_NAME = "TeamKist";
        private static readonly int DEFAULT_AUTO_SAVE_INTERVAL = 300;
        private static readonly int DEFAULT_BACKUP_COUNT = 10;
        private static readonly Boolean DEFAULT_LOAD_BACKUP_ON_START = true;
        private static readonly Boolean DEFAULT_KEEP_BACKUP_WITH_NO_EDITORS = true;

        //The settings applied to the program, either default or from the configuration file.
        public static IPAddress SERVER_IP;
        public static int SERVER_PORT;
        public static string SERVER_LEVEL_NAME;
        public static int SERVER_AUTO_SAVE_INTERVAL;
        public static int SERVER_BACKUP_COUNT;
        public static string SERVER_BASE_PATH;
        public static Boolean LOAD_BACKUP_ON_START;
        public static Boolean KEEP_BACKUP_WITH_NO_EDITORS;

        private static bool readyForShutdown = false;

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

            AppDomain.CurrentDomain.ProcessExit += CurrentDomain_ProcessExit;

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

            await using (var sp = services.BuildServiceProvider())
            {
                ILogger<Program> logger = sp.GetService<ILoggerFactory>().CreateLogger<Program>();

                TeamkistConfiguration teamkistConfiguration = sp.GetService<TeamkistConfiguration>();

                logger.LogInformation("Testing");
                teamkistConfiguration.LogConfiguration();
            }

            return;
           te(levelName, (current, c) => current.Replace(c, '_')).Replace(".zeeplevel", "");
                SERVER_LEVEL_NAME = string.IsNullOrWhiteSpace(levelName) ? DEFAULT_LEVEL_NAME : levelName;

                Log($"IP:\t\t\t{SERVER_IP}");
                Log($"Port:\t\t{SERVER_PORT}");
                Log($"Level Name:\t\t{SERVER_LEVEL_NAME}");
                Log($"Auto Save Interval:\t{SERVER_AUTO_SAVE_INTERVAL}");
                Log($"Backup Count:\t{SERVER_BACKUP_COUNT}");
                Log($"Loading Backup:\t{LOAD_BACKUP_ON_START}");
                Log($"Keeping Backup with No Editors:\t{KEEP_BACKUP_WITH_NO_EDITORS}");

                SERVER_BASE_PATH = AppDomain.CurrentDomain.BaseDirectory;

                TKEditor.Initialize();                
                TKServer.Initialize();
                TKSave.Initialize(LOAD_BACKUP_ON_START);

                while (true)
                {
                    TKSave.Run();
                    TKServer.Run();
                }
            }
            catch (Exception ex)
            {
                Log("The server has encountered the following error and will be stopped:");
                Log($"Error: {ex.Message}");

                TKServer.server?.Shutdown("Error");
                TKSave.Save();

                Log("Press enter to exit...");
                readyForShutdown = true;
                Console.ReadLine();
            }
        }

        private static void CurrentDomain_ProcessExit(object? sender, EventArgs e)
        {
            if (readyForShutdown)
            {
                Log("Exiting...");
            }

            TKServer.server?.Shutdown("Error");
            TKSave.Save();

            Log("Exiting...");
            Console.ReadLine();
        }

        public static void Log(string msg)
        {
            Console.WriteLine(" [TEAMKIST] " + msg);
        }

    }
}
