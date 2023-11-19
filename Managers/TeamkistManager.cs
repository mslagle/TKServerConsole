using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TKServerConsole.Configuration;

namespace TKServerConsole.Managers
{
    public class TeamkistManager
    {
        public ILogger<TeamkistManager> logger { get; set; }
        public TeamkistConfiguration configuration { get; set; }

        public TeamkistManager(ILogger<TeamkistManager> logger, TeamkistConfiguration configuration) { 
            this.logger = logger;
            this.configuration = configuration;


        }

        public void Run()
        {
            while ( true )
            {

            }
        }
    }
}
