using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TKServerConsole.Models
{
    public class TeamkistOptions
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
