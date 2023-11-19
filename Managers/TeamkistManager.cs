using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Timers;
using TKServerConsole.Configuration;
using TKServerConsole.Repositories;

namespace TKServerConsole.Managers
{
    public class TeamkistManager
    {
        public ILogger<TeamkistManager> logger { get; private set; }
        public TeamkistConfiguration configuration { get; private set; }
        public System.Timers.Timer saveTimer { get; private set; }

        public ISaveRepository saveRepository { get; private set; }
        public TeamkistServer server { get; private set; }
        public TKPlayerManager playerManager { get; private set; }
        public TKEditorState state { get; private set; }

        public TeamkistManager(ILogger<TeamkistManager> logger, TeamkistConfiguration configuration, 
            ISaveRepository saveRepository, TeamkistServer server, TKPlayerManager playerManager, TKEditorState state) { 
            this.logger = logger;
            this.configuration = configuration;

            this.saveRepository = saveRepository;
            this.server = server;
            this.playerManager = playerManager;
            this.state = state;
        }

        public void Instantiate()
        {
            // Load backup on start if set
            if (configuration.Options.LOAD_BACKUP_ON_START)
            {
                var latestSave = saveRepository.GetLatestSave();
                state.LoadSave(latestSave);
            }

            AppDomain.CurrentDomain.ProcessExit += Shutdown;
            saveTimer = new System.Timers.Timer();
            saveTimer.Interval = configuration.Options.AUTO_SAVE_INTERVAL * 1000;
            saveTimer.Elapsed += SaveTimer_Elapsed;
            saveTimer.Start();
        }

        private void SaveTimer_Elapsed(object? sender, ElapsedEventArgs e)
        {
            saveRepository.Save(state.GenerateSave());
        }

        public void Run()
        {
            while ( true )
            {
                server.Run();
            }
        }

        public void Shutdown(object? sender, EventArgs e)
        {
            logger.LogInformation("Shutting down server...");
            server.Shutdown();
            saveTimer?.Dispose();

            logger.LogInformation("Saving before shutdown...");
            saveRepository.Save(state.GenerateSave());

            logger.LogInformation("Shutdown ready and completed!");
        }
    }
}
