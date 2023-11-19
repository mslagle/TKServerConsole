using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using TKServerConsole.Models;

namespace TKServerConsole.Utils
{
    public static class TKUtilities
    {
        public static TKBlock JSONToTKBlock(string json)
        {
            TKBlock block = JsonConvert.DeserializeObject<TKBlock>(json);
            return block;
        }

        public static string GetJSONString(TKBlock tkBlock)
        {
            return JsonConvert.SerializeObject(tkBlock);
        }

        public static List<float> PropertyStringToList(string properties)
        {
            return properties.Split('|').Select(s => float.Parse(s, CultureInfo.InvariantCulture)).ToList();
        }

        public static string PropertyListToString(List<float> properties)
        {
            return string.Join("|", properties.Select(p => p.ToString(CultureInfo.InvariantCulture)));
        }
    }
}
