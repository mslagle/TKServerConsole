using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TKServerConsole.Utils;

namespace TKServerConsole.Models
{
    public class TKBlock
    {
        public Vector3 position { get; set; }
        public Vector3 eulerAngles { get; set; }
        public Vector3 localScale { get; set; }
        public List<float> properties { get; set; }
        public int blockID { get; set; }
        public string UID { get; set; }

        public TKBlock() { }

        public void AssignProperties(string properties)
        {
            List<float> propertyList = TKUtilities.PropertyStringToList(properties);
            this.position.x = propertyList[0];
            this.position.y = propertyList[1];
            this.position.z = propertyList[2];
            this.eulerAngles.x = propertyList[3];
            this.eulerAngles.y = propertyList[4];
            this.eulerAngles.z = propertyList[5];
            this.localScale.x = propertyList[6];
            this.localScale.y = propertyList[7];
            this.localScale.z = propertyList[8];
            this.properties = propertyList;
        }
    }
}
