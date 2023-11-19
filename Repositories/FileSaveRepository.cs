using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using TKServerConsole.Configuration;
using TKServerConsole.Models;

namespace TKServerConsole.Repositories
{
    public interface ISaveRepository
    {
        public TKSaveFile GetLatestSave();
        public void Save(TKSaveFile save);
    }


    public class FileSaveRepository : ISaveRepository
    {
        private ILogger<FileSaveRepository> logger { get; set; }
        private TeamkistConfiguration configuration { get; set; }

        public string ProjectPath { get; set; }
        public string ZeepSavePath { get; set; }
        public string ServerSavePath { get; set; }

        public FileSaveRepository(ILogger<FileSaveRepository> logger, TeamkistConfiguration configuration)
        {
            this.logger = logger;
            this.configuration = configuration;

            string serverBasePath = AppDomain.CurrentDomain.BaseDirectory;

            ProjectPath = serverBasePath;// Path.Combine(serverBasePath, configuration.Options.LEVEL_NAME);
            ZeepSavePath = Path.Combine(ProjectPath, "ZeepSaves");
            ServerSavePath = Path.Combine(ProjectPath, "ServerSaves");

            if (!Directory.Exists(ProjectPath))
            {
                Directory.CreateDirectory(ProjectPath);
            }
            if (!Directory.Exists(ZeepSavePath))
            {
                Directory.CreateDirectory(ZeepSavePath);
            }
            if (!Directory.Exists(ServerSavePath))
            {
                Directory.CreateDirectory(ServerSavePath);
            }
        }

        public TKSaveFile GetLatestSave()
        {
            logger.LogInformation($"Getting latest save file from {ServerSavePath}");

            DirectoryInfo serverSavesDirectory = new DirectoryInfo(ServerSavePath);
            FileInfo[] saves = serverSavesDirectory.GetFiles("*.teamkist", SearchOption.TopDirectoryOnly).OrderByDescending(x => x.CreationTime).ToArray();
            FileInfo latestSave = saves.FirstOrDefault();

            if (latestSave != null)
            {
                logger.LogInformation($"Latest save found at {latestSave.FullName}");
                string jsonString = File.ReadAllText(latestSave.FullName);
                TKSaveFile saveFile = JsonConvert.DeserializeObject<TKSaveFile>(jsonString);
                return saveFile;
            }

            return new TKSaveFile();
        }

        public void Save(TKSaveFile save)
        {
            // Save as a serverFile 1st
            string jsonString = JsonConvert.SerializeObject(save);
            string filePath = GetTimestampedFilePath(ServerSavePath, configuration.Options.LEVEL_NAME + ".teamkist");
            File.WriteAllText(filePath, jsonString);
            logger.LogDebug($"Saved current state to {filePath}");

            SaveZeepLevel(save);
        }

        public void SaveZeepLevel(TKSaveFile save)
        {
            //Ready to go! Create a 12 digit random number for the UID.
            Random random = new Random();
            string randomNumber = "";

            for (int i = 0; i < 12; i++)
            {
                randomNumber += random.Next(0, 10).ToString();
            }

            string parsedName = Regex.Replace(configuration.Options.LEVEL_NAME, @"[^a-zA-Z0-9\s]", "");

            //Create the complete UID.
            DateTime now = DateTime.Now;
            string UID = now.Day.ToString("00") + now.Month.ToString("00") + now.Year.ToString() + "-" + now.Hour.ToString("00") + now.Minute.ToString("00") + now.Second.ToString("00") + now.Millisecond.ToString("000") + "-" + parsedName + "-" + randomNumber + "-" + save.blocks.Count;

            //Create the list to hold the file.
            List<string> fileLines = new List<string>();

            //Create the header.
            fileLines.Add($"LevelEditor2,{parsedName},{UID}");
            fileLines.Add("0,0,0,0,0,0,0,0");
            fileLines.Add($"invalid track,0,0,0,{save.skybox},{save.floor}");

            foreach (TKBlock block in save.blocks)
            {
                fileLines.Add($"{block.blockID.ToString()},{string.Join(",", block.properties.Select(p => p.ToString(CultureInfo.InvariantCulture)))}");
            }

            string zeepPath = GetTimestampedFilePath(ZeepSavePath, configuration.Options.LEVEL_NAME + ".zeeplevel");
            File.WriteAllLines(zeepPath, fileLines);

            logger.LogDebug($"Saved current backup to {zeepPath}");
        }

        public string GetTimestampedFilePath(string directoryPath, string fileName)
        {
            // Create a timestamp string in the format "yyyyMMdd-HHmmss"
            string timestamp = DateTime.Now.ToString("yyyyMMdd-HHmmss");

            // Get the file extension (if any) from the file name
            string extension = Path.GetExtension(fileName);

            // Create a new file name with the timestamp and extension (if any)
            string timestampedFileName = Path.GetFileNameWithoutExtension(fileName) + "_" + timestamp + extension;

            // Combine the directory path and timestamped file name to get the full file path
            string timestampedFilePath = Path.Combine(directoryPath, timestampedFileName);

            return timestampedFilePath;
        }

        public void ManageSaveFileCount()
        {
            DirectoryInfo serverSaveDirectory = new DirectoryInfo(ServerSavePath);
            FileInfo[] saves = serverSaveDirectory.GetFiles("*.teamkist", SearchOption.TopDirectoryOnly);
            if (saves.Length > configuration.Options.BACKUP_COUNT)
            {
                saves = saves.OrderByDescending(f => f.CreationTime).ToArray();
                FileInfo last = saves.Last();

                if (File.Exists(last.FullName))
                {
                    File.Delete(last.FullName);
                }
            }
        }

        public void ManageZeepSaveFileCount()
        {
            DirectoryInfo zeepSaveDirectory = new DirectoryInfo(ZeepSavePath);
            FileInfo[] saves = zeepSaveDirectory.GetFiles("*.zeeplevel", SearchOption.TopDirectoryOnly);
            if (saves.Length > configuration.Options.BACKUP_COUNT)
            {
                saves = saves.OrderByDescending(f => f.CreationTime).ToArray();
                FileInfo last = saves.Last();

                if (File.Exists(last.FullName))
                {
                    File.Delete(last.FullName);
                }
            }
        }
    }
}
