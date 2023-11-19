using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TKServerConsole.Utils;

namespace TKServerConsole.Models
{
    public class TKSaveFile
    {
        public int floor;
        public int skybox;
        public List<TKBlock> blocks;

        public TKSaveFile()
        {
            floor = 90;
            skybox = 0;
            blocks = new List<TKBlock>();
        }
    }
}
