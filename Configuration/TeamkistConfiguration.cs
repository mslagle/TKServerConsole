using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TKServerConsole.Models;

namespace TKServerConsole.Configuration
{
    public class TeamkistConfiguration
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
}
