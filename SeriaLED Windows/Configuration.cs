using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;

namespace SeriaLED.Configs
{
    public class Configuration
    {
        public string Address;
        public string Username;
        public string Password;
        public string ClientID = "serialed-daemon";
        public string Topic;
        public string COMPort;
        //public string TopicOut;
        //public List<Sensor> Sensors;
        public Configuration()
        {
            //Sensors = new List<Sensor>();
        }
        public void Save()
        {
            File.WriteAllText(Path.GetDirectoryName(Assembly.GetEntryAssembly().Location) + "/myconfig.json", JsonConvert.SerializeObject(this));
        }
    }
}
