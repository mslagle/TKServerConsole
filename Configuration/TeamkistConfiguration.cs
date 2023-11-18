using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TKServerConsole.Configuration
{
    internal class TeamkistConfiguration
    {
        private readonly ILogger<TeamkistConfiguration> logger;
        public TeamkistOptions Options { get; set; }

        public TeamkistConfiguration(IConfiguration configuration, ILogger<TeamkistConfiguration> logger) {
            this.logger = logger;

            // Get the configuration section
            var teamkistSection = configuration.GetSection("Teamkist");

            // Bind the section to the object
            this.Options = new TeamkistOptions();
            teamkistSection.Bind(this.Options);
        }

        public void LogConfiguration()
        {
            int padding = 25;

            logger.LogInformation($"{"IP:".PadRight(padding)}{Options.SERVER_IP}");
            logger.LogInformation($"{"PORT:".PadRight(padding)}{Options.SERVER_PORT}");
            logger.LogInformation($"{"LEVEL NAME:".PadRight(padding)}{Options.LEVEL_NAME}");
            logger.LogInformation($"{"AUTO SAVE INTERVAL:".PadRight(padding)}{Options.AUTO_SAVE_INTERVAL}");
            logger.LogInformation($"{"BACKUP COUNT:".PadRight(padding)}{Options.BACKUP_COUNT}");
            logger.LogInformation($"{"LOADING_BACKUP:".PadRight(padding)}{Options.LOAD_BACKUP_ON_START}");
            logger.LogInformation($"{"KEEPING BACKUP WITH NO EDITORS:".PadRight(padding)}{Options.KEEP_BACKUP_WITH_NO_EDITORS}");
        }
    }

    internal class TeamkistOptions
    {
        public string SERVER_IP { get; set; }
        public int SERVER_PORT { get; set; }
        public string LEVEL_NAME { get; set; }
        public int AUTO_SAVE_INTERVAL { get; set; }
        public int BACKUP_COUNT { get; set; }
        public bool LOAD_BACKUP_ON_START { get; set; }
        public bool KEEP_BACKUP_WITH_NO_EDITORS { get; set; }

        public TeamkistOptions()
        {
            // Set defaults
            this.SERVER_IP = "0.0.0.0";
            this.SERVER_PORT = 50000;
            this.LEVEL_NAME = "TeamkistLevel";
            this.AUTO_SAVE_INTERVAL = 60;
            this.BACKUP_COUNT = 5;
            this.LOAD_BACKUP_ON_START = true;
            this.KEEP_BACKUP_WITH_NO_EDITORS = true;
        }
    }
}
