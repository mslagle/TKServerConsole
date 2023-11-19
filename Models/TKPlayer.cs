using Lidgren.Network;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TKServerConsole.Models
{
    public class TKPlayer
    {
        public int ID;
        public string name;
        public int hat;
        public int color;
        public int soapbox;
        public NetConnection connection;
        public byte state;
    }
}
