﻿using System;
using System.Linq;
using System.Net;
using System.Configuration;
using System.IO;
using System.Runtime.InteropServices;
using System.Diagnostics;
using System.Globalization;

namespace TKServerConsole
{
    public static class Program
    {
        //Default Settings.
        private static readonly IPAddress DEFAULT_IP = IPAddress.Parse((string)"127.0.0.1");
        private static readonly int DEFAULT_PORT = 50000;
        private static readonly string DEFAULT_LEVEL_NAME = "TeamKist";
        private static readonly int DEFAULT_AUTO_SAVE_INTERVAL = 300;
        private static readonly int DEFAULT_BACKUP_COUNT = 10;

        //The settings applied to the program, either default or from the configuration file.
        public static IPAddress SERVER_IP;
        public static int SERVER_PORT;
        public static string SERVER_LEVEL_NAME;
        public static int SERVER_AUTO_SAVE_INTERVAL;
        public static int SERVER_BACKUP_COUNT;
        public static string SERVER_BASE_PATH;

        private static bool readyForShutdown = false;

        private static string[] logo = new string[]
        {
            @"    _____ ___   _   __  __ _  _____ ___ _____ ",
            @"   |_   _| __| /_\ |  \/  | |/ /_ _/ __|_   _|",
            @"     | | | _| / _ \| |\/| | ' < | |\__ \ | |  ",
            @"     |_| |___/_/ \_\_|  |_|_|\_\___|___/ |_|  ",
        };

        public static void Main(string[] args)
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

            handler = new ConsoleEventDelegate(ConsoleEventCallback);
            SetConsoleCtrlHandler(handler, true);

            try
            {
                Log("Starting Teamkist Server V1.5");
                Log("Reading configuration file.");

                var serverIpString = ConfigurationManager.AppSettings["ServerIP"];
                var serverPortString = ConfigurationManager.AppSettings["ServerPort"];
                var levelName = ConfigurationManager.AppSettings["LevelName"];
                var autoSaveIntervalString = ConfigurationManager.AppSettings["AutoSaveInterval"];
                var backupCountString = ConfigurationManager.AppSettings["BackupCount"];

                SERVER_IP = string.IsNullOrWhiteSpace(serverIpString) ? DEFAULT_IP : IPAddress.Parse(serverIpString);
                SERVER_PORT = string.IsNullOrWhiteSpace(serverPortString) ? DEFAULT_PORT : int.Parse(serverPortString);
                SERVER_AUTO_SAVE_INTERVAL = string.IsNullOrWhiteSpace(autoSaveIntervalString) ? DEFAULT_AUTO_SAVE_INTERVAL : int.Parse(autoSaveIntervalString);
                SERVER_BACKUP_COUNT = string.IsNullOrWhiteSpace(backupCountString) ? DEFAULT_BACKUP_COUNT : int.Parse(backupCountString);

                levelName = Path.GetInvalidFileNameChars().Aggregate(levelName, (current, c) => current.Replace(c, '_')).Replace(".zeeplevel", "");
                SERVER_LEVEL_NAME = string.IsNullOrWhiteSpace(levelName) ? DEFAULT_LEVEL_NAME : levelName;

                Log($"IP:\t\t\t{SERVER_IP}");
                Log($"Port:\t\t{SERVER_PORT}");
                Log($"Level Name:\t\t{SERVER_LEVEL_NAME}");
                Log($"Auto Save Interval:\t{SERVER_AUTO_SAVE_INTERVAL}");
                Log($"Backup Count:\t{SERVER_BACKUP_COUNT}");

                SERVER_BASE_PATH = AppDomain.CurrentDomain.BaseDirectory;

                TKEditor.Initialize();                
                TKServer.Initialize();
                TKSave.Initialize();

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

        public static void Log(string msg)
        {
            Console.WriteLine(" [TEAMKIST] " + msg);
        }

        static bool ConsoleEventCallback(int eventType)
        {
            if (readyForShutdown)
            {
                Log("Exiting...");
                return false;
            }

            TKServer.server?.Shutdown("Error");
            TKSave.Save();

            Log("Exiting...");
            Console.ReadLine();
            return false;
        }
        static ConsoleEventDelegate handler;   // Keeps it from getting garbage collected
                                               // Pinvoke
        private delegate bool ConsoleEventDelegate(int eventType);
        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool SetConsoleCtrlHandler(ConsoleEventDelegate callback, bool add);
    }
}
